

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;
using GamePaymentSDK.Services;
using GamePaymentSDK.Storage;
using GamePaymentSDK.WebView;
using UnityEngine;

namespace GamePaymentSDK.Direct
{
    public sealed class GamePaymentController : IGamePaymentController
    {
        public event Action<PaymentInitializedEventArgs> Initialized;
        public event Action<IReadOnlyCollection<PaymentProduct>> ProductsUpdated;
        public event Action<PaymentPurchaseResult> PurchaseSucceeded;
        public event Action<PaymentPurchaseFailedEventArgs> PurchaseFailed;

        private readonly PaymentSettings _settings;
        private readonly string _playerId;
        private readonly ILogger _logger;
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
            PaymentSettings settings,
            string playerId,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            _settings = settings;
            _playerId = playerId;
            _logger = logger;
            _apiClient = new PaymentApiClient(settings, _logger);

            _pendingOrderStorage = new PlayerPrefsPendingOrderStorage(playerId, _logger);

            _processedTransactionStorage = new PlayerPrefsProcessedTransactionStorage(playerId, _logger);

            _localCleanupService = new PaymentLocalCleanupService(
                settings,
                _pendingOrderStorage,
                _processedTransactionStorage,
                _logger
            );

            _productCatalogCache = new ProductCatalogCache(_logger);

            _productCatalogService = new ProductCatalogService(
                _apiClient,
                _productCatalogCache,
                _logger
            );

            _paymentClaimService = new PaymentClaimService(
                _apiClient,
                _pendingOrderStorage,
                _logger
            );

            _paymentRequestService = new PaymentRequestService(
                _apiClient,
                _productCatalogService,
                _pendingOrderStorage,
                _logger
            );

            _callbackParser = new PaymentCallbackParser(_logger);

            _purchaseFlowService = new PaymentPurchaseFlowService(
                settings,
                _paymentRequestService,
                _paymentClaimService,
                webViewService,
                _callbackParser,
                _logger
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
                PaymentInitializedEventArgs failedArgs = new(
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
                _logger.Log(LogType.Log, "[PaymentSdk] [GamePayment] Initialization started.");

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
                 * Recovery(Claim) should not block initialization success.
                 * Product catalog is ready now. Pending claim recovery runs after that.
                 */
                PaymentResult<List<PaymentPurchaseResult>> recoveryResult =
                    await ClaimPendingPurchasesAsync();

                if (!recoveryResult.Success)
                {
                    _logger.Log(LogType.Warning, $"[PaymentSdk] [GamePayment] Pending purchase recovery failed during initialize. reason={recoveryResult.FailureReason}, error={recoveryResult.ErrorMessage}");
                }

                _logger.Log(LogType.Log, "[PaymentSdk] [GamePayment] Initialization completed.");

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

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(string productKey)
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
                    new(
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
                _logger.Log(LogType.Warning, "[PaymentSdk] [GamePayment] Cannot confirm null purchase.");
                return;
            }

            if (string.IsNullOrWhiteSpace(purchase.TransactionId))
            {
                _logger.Log(LogType.Warning, $"[PaymentSdk] [GamePayment] Cannot confirm purchase without transactionId. orderId={purchase.OrderId}, productKey={purchase.ProductKey}");
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

            _logger.Log(LogType.Log, "[PaymentSdk] [GamePayment] GamePaymentController disposed.");
        }

        private PaymentResult ValidateBeforeInitialize()
        {
            if (_settings == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "PaymentSettings is null."
                );
            }

            if (!_settings.IsValid(out string configError))
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
                    "PlayerId is required."
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
                    _logger.Log(LogType.Warning, $"[PaymentSdk] [GamePayment] Purchase result has empty transactionId. productKey={purchase.ProductKey}, orderId={purchase.OrderId}");

                    continue;
                }

                if (_processedTransactionStorage.IsProcessed(purchase.TransactionId))
                {
                    _logger.Log(LogType.Log, $"[PaymentSdk] [GamePayment] Purchase success skipped because transaction is already processed. transactionId={purchase.TransactionId}, productKey={purchase.ProductKey}");

                    continue;
                }

                PurchaseSucceeded?.Invoke(purchase);
            }
        }
    }
}