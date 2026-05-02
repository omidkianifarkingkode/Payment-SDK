/// Initialization Behavior:
/// 1. Validate config/playerId.
/// 2. GET /v1/products.
/// 3. Store products in RAM.
/// 4. Raise ProductsUpdated.
/// 5. Raise Initialized(success).
/// 6. Call pending claim recovery.
/// 7. Raise PurchaseSucceeded for recovered purchases.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;
using GamePaymentSDK.Services;
using GamePaymentSDK.Storage;
using GamePaymentSDK.WebView;

namespace GamePaymentSDK.Direct
{
    public sealed class GamePaymentController : IGamePaymentController
    {
        public event Action<PaymentInitializedEventArgs> Initialized;
        public event Action<IReadOnlyCollection<PaymentProduct>> ProductsUpdated;
        public event Action<PaymentPurchaseResult> PurchaseSucceeded;
        public event Action<PaymentPurchaseFailedEventArgs> PurchaseFailed;

        private readonly PaymentConfiguration _configuration;
        private readonly string _playerId;

        private readonly IPaymentApiClient _apiClient;
        private readonly IPendingOrderStorage _pendingOrderStorage;
        private readonly IProcessedTransactionStorage _processedTransactionStorage;
        private readonly ProductCatalogCache _productCatalogCache;

        private readonly IProductCatalogService _productCatalogService;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IPaymentCallbackParser _callbackParser;
        private readonly IPaymentPurchaseFlowService _purchaseFlowService;
        private readonly PaymentLocalCleanupService _localCleanupService;

        private bool _isInitialized;
        private bool _isInitializing;
        private bool _isDisposed;

        public bool IsInitialized => _isInitialized;
        public bool IsInitializing => _isInitializing;

        public bool IsPurchaseInProgress =>
            _purchaseFlowService != null &&
            _purchaseFlowService.IsPurchaseInProgress;

        public string PlayerId => _playerId;

        public GamePaymentController(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService
        )
        {
            _configuration = configuration;
            _playerId = playerId;

            PaymentLogger.SetEnabled(configuration?.EnableLogs ?? true);

            _apiClient = new PaymentApiClient(configuration);

            _pendingOrderStorage = new PlayerPrefsPendingOrderStorage(
                configuration?.ClientId,
                playerId
            );

            _processedTransactionStorage = new PlayerPrefsProcessedTransactionStorage(
                configuration?.ClientId,
                playerId
            );

            _localCleanupService = new PaymentLocalCleanupService(
                configuration,
                _pendingOrderStorage,
                _processedTransactionStorage
            );

            _productCatalogCache = new ProductCatalogCache();

            _productCatalogService = new ProductCatalogService(
                _apiClient,
                _productCatalogCache
            );

            _paymentClaimService = new PaymentClaimService(
                _apiClient,
                _pendingOrderStorage
            );

            _paymentRequestService = new PaymentRequestService(
                _apiClient,
                _productCatalogService,
                _pendingOrderStorage
            );

            _callbackParser = new PaymentCallbackParser(configuration);

            _purchaseFlowService = new PaymentPurchaseFlowService(
                configuration,
                _paymentRequestService,
                _paymentClaimService,
                webViewService,
                _callbackParser
            );
        }

        public async Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> InitializeAsync()
        {
            if (_isDisposed)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.Unknown,
                    "GamePaymentController is disposed."
                );
            }

            if (_isInitialized)
            {
                IReadOnlyCollection<PaymentProduct> cachedProducts =
                    _productCatalogService.GetProducts();

                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Ok(
                    cachedProducts
                );
            }

            if (_isInitializing)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.StoreUnavailable,
                    "GamePayment initialization is already running."
                );
            }

            PaymentResult validation = ValidateBeforeInitialize();

            if (!validation.Success)
            {
                PaymentInitializedEventArgs failedArgs = new PaymentInitializedEventArgs(
                    false,
                    Array.Empty<PaymentProduct>(),
                    validation.FailureReason,
                    validation.ErrorMessage
                );

                Initialized?.Invoke(failedArgs);

                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    validation.FailureReason,
                    validation.ErrorMessage
                );
            }

            _isInitializing = true;

            try
            {
                PaymentLogger.Log("GamePayment initialization started.");

                _localCleanupService.RunCleanup();

                PaymentResult<IReadOnlyCollection<PaymentProduct>> productsResult =
                    await _productCatalogService.InitializeAsync();

                if (!productsResult.Success)
                {
                    _isInitialized = false;

                    Initialized?.Invoke(new PaymentInitializedEventArgs(
                        false,
                        Array.Empty<PaymentProduct>(),
                        productsResult.FailureReason,
                        productsResult.ErrorMessage
                    ));

                    return productsResult;
                }

                _isInitialized = true;

                IReadOnlyCollection<PaymentProduct> products = productsResult.Data;

                ProductsUpdated?.Invoke(products);

                Initialized?.Invoke(new PaymentInitializedEventArgs(
                    true,
                    products,
                    PaymentFailureReason.None,
                    null
                ));

                /*
                 * Recovery should not block initialization success.
                 * Product catalog is ready now. Pending claim recovery runs after that.
                 */
                PaymentResult<List<PaymentPurchaseResult>> recoveryResult =
                    await ClaimPendingPurchasesAsync();

                if (!recoveryResult.Success)
                {
                    PaymentLogger.LogWarning(
                        $"Pending purchase recovery failed during initialize. reason={recoveryResult.FailureReason}, error={recoveryResult.ErrorMessage}"
                    );
                }

                PaymentLogger.Log("GamePayment initialization completed.");

                return productsResult;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public IReadOnlyCollection<PaymentProduct> GetProducts()
        {
            if (_isDisposed)
                return Array.Empty<PaymentProduct>();

            return _productCatalogService.GetProducts();
        }

        public bool TryGetProduct(string productKey, out PaymentProduct product)
        {
            product = null;

            if (_isDisposed)
                return false;

            return _productCatalogService.TryGetProduct(productKey, out product);
        }

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(
            string productKey
        )
        {
            if (_isDisposed)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.Unknown,
                    "GamePaymentController is disposed."
                );
            }

            if (!_isInitialized)
            {
                PaymentPurchaseFailedEventArgs failedArgs =
                    new PaymentPurchaseFailedEventArgs(
                        productKey,
                        PaymentFailureReason.NotInitialized,
                        "GamePayment is not initialized."
                    );

                PurchaseFailed?.Invoke(failedArgs);

                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.NotInitialized,
                    "GamePayment is not initialized."
                );
            }

            PaymentResult<List<PaymentPurchaseResult>> result =
                await _purchaseFlowService.PurchaseAsync(_playerId, productKey);

            if (result.Success)
            {
                RaisePurchaseSucceededEvents(result.Data);
            }
            else
            {
                PurchaseFailed?.Invoke(new PaymentPurchaseFailedEventArgs(
                    productKey,
                    result.FailureReason,
                    result.ErrorMessage
                ));
            }

            return result;
        }

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimPendingPurchasesAsync()
        {
            if (_isDisposed)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.Unknown,
                    "GamePaymentController is disposed."
                );
            }

            PaymentResult<List<PaymentPurchaseResult>> result =
                await _paymentClaimService.ClaimLocalPendingOrdersAsync(_playerId);

            if (result.Success)
            {
                RaisePurchaseSucceededEvents(result.Data);
            }

            return result;
        }

        public void ConfirmPurchaseProcessed(PaymentPurchaseResult purchase)
        {
            if (purchase == null)
            {
                PaymentLogger.LogWarning("Cannot confirm null purchase.");
                return;
            }

            if (string.IsNullOrWhiteSpace(purchase.TransactionId))
            {
                PaymentLogger.LogWarning(
                    $"Cannot confirm purchase without transactionId. orderId={purchase.OrderId}, productKey={purchase.ProductKey}"
                );

                return;
            }

            _processedTransactionStorage.MarkProcessed(
                purchase.TransactionId,
                purchase.OrderId,
                purchase.ProductKey
            );
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            Initialized = null;
            ProductsUpdated = null;
            PurchaseSucceeded = null;
            PurchaseFailed = null;

            PaymentLogger.Log("GamePaymentController disposed.");
        }

        private PaymentResult ValidateBeforeInitialize()
        {
            if (_configuration == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "PaymentConfiguration is null."
                );
            }

            if (!_configuration.IsValid(out string configError))
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    configError
                );
            }

            if (string.IsNullOrWhiteSpace(_playerId))
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            return PaymentResult.Ok();
        }

        private void RaisePurchaseSucceededEvents(List<PaymentPurchaseResult> purchases)
        {
            if (purchases == null)
                return;

            foreach (PaymentPurchaseResult purchase in purchases)
            {
                if (purchase == null)
                    continue;

                if (string.IsNullOrWhiteSpace(purchase.TransactionId))
                {
                    PaymentLogger.LogWarning(
                        $"Purchase result has empty transactionId. productKey={purchase.ProductKey}, orderId={purchase.OrderId}"
                    );

                    continue;
                }

                if (_processedTransactionStorage.IsProcessed(purchase.TransactionId))
                {
                    PaymentLogger.Log(
                        $"Purchase success skipped because transaction is already processed. transactionId={purchase.TransactionId}, productKey={purchase.ProductKey}"
                    );

                    continue;
                }

                PurchaseSucceeded?.Invoke(purchase);
            }
        }
    }
}