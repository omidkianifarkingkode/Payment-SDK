using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GamePaymentSDK.Core;
using GamePaymentSDK.Direct;
using GamePaymentSDK.WebView;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityPurchaseFailureReason = UnityEngine.Purchasing.PurchaseFailureReason;

namespace GamePaymentSDK.UnityIAP
{
    public sealed class GamePaymentIapStore : IStore, IDisposable
    {
        private readonly PaymentConfiguration _configuration;
        private readonly string _playerId;
        private readonly IPaymentWebViewService _webViewService;

        private IStoreCallback _callback;
        private GamePaymentController _controller;

        private ReadOnlyCollection<ProductDefinition> _requestedProducts;
        private bool _isRetrievingProducts;

        public GamePaymentIapStore(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService
        )
        {
            _configuration = configuration;
            _playerId = playerId;
            _webViewService = webViewService;
        }

        public void Initialize(IStoreCallback callback)
        {
            _callback = callback;

            PaymentLogger.Log("GamePaymentIapStore initialized by Unity IAP.");
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
        {
            _requestedProducts = products;

            if (_isRetrievingProducts)
            {
                PaymentLogger.LogWarning("RetrieveProducts ignored because retrieval is already running.");
                return;
            }

            _ = RetrieveProductsAsync(products);
        }

        public void Purchase(ProductDefinition product, string developerPayload)
        {
            if (product == null)
            {
                NotifyPurchaseFailed(
                    null,
                    UnityPurchaseFailureReason.ProductUnavailable,
                    "ProductDefinition is null."
                );

                return;
            }

            _ = PurchaseAsync(product);
        }

        public void FinishTransaction(ProductDefinition product, string transactionId)
        {
            string productId = product?.storeSpecificId ?? product?.id ?? "unknown";
        
            PaymentLogger.Log(
                $"FinishTransaction called. productId={productId}, transactionId={transactionId}"
            );
        
            if (_controller == null)
            {
                PaymentLogger.LogWarning(
                    $"Cannot confirm processed transaction because controller is null. transactionId={transactionId}"
                );
        
                return;
            }
        
            PaymentPurchaseResult purchase = new PaymentPurchaseResult
            {
                TransactionId = transactionId,
                OrderId = transactionId,
                ProductKey = productId,
                Receipt = null
            };
        
            _controller.ConfirmPurchaseProcessed(purchase);
        }

        private async System.Threading.Tasks.Task RetrieveProductsAsync(
            ReadOnlyCollection<ProductDefinition> requestedProducts
        )
        {
            _isRetrievingProducts = true;

            try
            {
                if (_callback == null)
                {
                    PaymentLogger.LogError("Cannot retrieve products because IStoreCallback is null.");
                    return;
                }

                PaymentResult validationResult = ValidateDependencies();

                if (!validationResult.Success)
                {
                    _callback.OnSetupFailed(
                        InitializationFailureReason.PurchasingUnavailable,
                        validationResult.ErrorMessage
                    );

                    return;
                }

                EnsureController();

                PaymentResult<IReadOnlyCollection<PaymentProduct>> initResult =
                    await _controller.InitializeAsync();

                if (!initResult.Success)
                {
                    _callback.OnSetupFailed(
                        InitializationFailureReason.PurchasingUnavailable,
                        initResult.ErrorMessage
                    );

                    return;
                }

                List<ProductDescription> descriptions =
                    BuildProductDescriptions(requestedProducts, initResult.Data);

                _callback.OnProductsRetrieved(descriptions);

                PaymentLogger.Log(
                    $"Unity IAP products retrieved. Count={descriptions.Count}"
                );
            }
            catch (Exception exception)
            {
                PaymentLogger.LogError($"RetrieveProductsAsync exception: {exception}");

                _callback?.OnSetupFailed(
                    InitializationFailureReason.PurchasingUnavailable,
                    exception.Message
                );
            }
            finally
            {
                _isRetrievingProducts = false;
            }
        }

        private async System.Threading.Tasks.Task PurchaseAsync(ProductDefinition product)
        {
            string productKey = GetProductKey(product);

            try
            {
                if (_callback == null)
                {
                    PaymentLogger.LogError("Cannot purchase because IStoreCallback is null.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(productKey))
                {
                    NotifyPurchaseFailed(
                        productKey,
                        UnityPurchaseFailureReason.ProductUnavailable,
                        "Product key is empty."
                    );

                    return;
                }

                EnsureController();

                if (!_controller.IsInitialized)
                {
                    PaymentResult<IReadOnlyCollection<PaymentProduct>> initResult =
                        await _controller.InitializeAsync();

                    if (!initResult.Success)
                    {
                        NotifyPurchaseFailed(
                            productKey,
                            UnityPurchaseFailureReason.PurchasingUnavailable,
                            initResult.ErrorMessage
                        );

                        return;
                    }
                }

                PaymentResult<List<PaymentPurchaseResult>> purchaseResult =
                    await _controller.PurchaseAsync(productKey);

                if (!purchaseResult.Success)
                {
                    /*
                     * GamePaymentController.PurchaseAsync already raised PurchaseFailed.
                     * This branch is kept only for safety if the event path changes later.
                     */
                    return;
                }

                if (purchaseResult.Data == null || purchaseResult.Data.Count == 0)
                {
                    NotifyPurchaseFailed(
                        productKey,
                        UnityPurchaseFailureReason.Unknown,
                        "Purchase succeeded internally but returned no purchase result."
                    );

                    return;
                }

                /*
                 * Success is forwarded by HandleControllerPurchaseSucceeded.
                 * Do not call _callback.OnPurchaseSucceeded here, otherwise Unity IAP gets duplicate transactions.
                 */
            }
            catch (Exception exception)
            {
                PaymentLogger.LogError($"PurchaseAsync exception: {exception}");

                NotifyPurchaseFailed(
                    productKey,
                    UnityPurchaseFailureReason.Unknown,
                    exception.Message
                );
            }
        }

        private void EnsureController()
        {
            if (_controller != null)
                return;

            _controller = new GamePaymentController(
                _configuration,
                _playerId,
                _webViewService
            );

            _controller.PurchaseSucceeded += HandleControllerPurchaseSucceeded;
            _controller.PurchaseFailed += HandleControllerPurchaseFailed;
        }

        private PaymentResult ValidateDependencies()
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

            if (_webViewService == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentWebViewService is null."
                );
            }

            return PaymentResult.Ok();
        }

        private List<ProductDescription> BuildProductDescriptions(
            ReadOnlyCollection<ProductDefinition> requestedProducts,
            IReadOnlyCollection<PaymentProduct> backendProducts
        )
        {
            List<ProductDescription> descriptions = new();

            if (requestedProducts == null || backendProducts == null)
                return descriptions;

            Dictionary<string, PaymentProduct> backendByKey =
                backendProducts
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ProductKey))
                    .ToDictionary(x => x.ProductKey, x => x);

            foreach (ProductDefinition definition in requestedProducts)
            {
                if (definition == null)
                    continue;

                string productKey = GetProductKey(definition);

                if (string.IsNullOrWhiteSpace(productKey))
                    continue;

                if (!backendByKey.TryGetValue(productKey, out PaymentProduct paymentProduct))
                {
                    PaymentLogger.LogWarning(
                        $"Unity IAP requested product not found in backend catalog. productKey={productKey}"
                    );

                    continue;
                }

                ProductMetadata metadata = new ProductMetadata(
                    paymentProduct.GetLocalizedPriceString(),
                    paymentProduct.Name,
                    paymentProduct.Name,
                    paymentProduct.Currency,
                    ConvertPriceToDecimal(paymentProduct.Price)
                );

                ProductDescription description = new ProductDescription(
                    productKey,
                    metadata
                );

                descriptions.Add(description);
            }

            return descriptions;
        }

        private decimal ConvertPriceToDecimal(double price)
        {
            try
            {
                return Convert.ToDecimal(price);
            }
            catch
            {
                return 0m;
            }
        }

        private string GetProductKey(ProductDefinition product)
        {
            if (product == null)
                return null;

            if (!string.IsNullOrWhiteSpace(product.storeSpecificId))
                return product.storeSpecificId;

            return product.id;
        }

        private void NotifyPurchaseFailed(
            string productId,
            UnityPurchaseFailureReason reason,
            string message
        )
        {
            productId ??= string.Empty;
            message ??= "Purchase failed.";

            PaymentLogger.LogWarning(
                $"Unity IAP purchase failed. productId={productId}, reason={reason}, message={message}"
            );

            PurchaseFailureDescription description =
                new PurchaseFailureDescription(productId, reason, message);

            _callback?.OnPurchaseFailed(description);
        }

        private void HandleControllerPurchaseSucceeded(PaymentPurchaseResult purchase)
        {
            if (purchase == null)
                return;

            if (string.IsNullOrWhiteSpace(purchase.ProductKey))
                return;

            if (_callback == null)
            {
                PaymentLogger.LogWarning(
                    $"Unity IAP callback is null. Cannot forward purchase success. productKey={purchase.ProductKey}"
                );

                return;
            }

            _callback.OnPurchaseSucceeded(
                purchase.ProductKey,
                purchase.Receipt,
                purchase.TransactionId
            );

            PaymentLogger.Log(
                $"Unity IAP purchase success forwarded. productKey={purchase.ProductKey}, transactionId={purchase.TransactionId}"
            );
        }

        private void HandleControllerPurchaseFailed(PaymentPurchaseFailedEventArgs args)
        {
            if (args == null)
                return;

            NotifyPurchaseFailed(
                args.ProductKey,
                GamePaymentIapFailureMapper.ToUnityFailureReason(args.FailureReason),
                args.ErrorMessage
            );
        }

        public void Dispose()
        {
            if (_controller != null)
            {
                _controller.PurchaseSucceeded -= HandleControllerPurchaseSucceeded;
                _controller.PurchaseFailed -= HandleControllerPurchaseFailed;
                _controller.Dispose();
                _controller = null;
            }

            _callback = null;
        }
    }
}