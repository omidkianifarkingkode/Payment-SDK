using System.Collections.Generic;
using GamePaymentSDK.Core;
using GamePaymentSDK.Direct;
using GamePaymentSDK.WebView;
using GamePaymentSDK.WebView.Mock;
using UnityEngine;

namespace GamePaymentSDK.Samples
{
    public sealed class PaymentBootstrapDirect : MonoBehaviour
    {
        [Header("Payment Service")]
        [Tooltip("Payment data asset. If left empty, loads Resources/" + PaymentSettings.DefaultResourcePath + ".")]
        [SerializeField] private PaymentSettings _settings;

        [Header("WebView")]
        [Tooltip("Assign UniWebViewPaymentService here for device builds.")]
        [SerializeField] private MonoBehaviour _deviceWebViewService;

        [Tooltip("Assign MockPaymentWebViewService here for Editor testing.")]
        [SerializeField] private MockPaymentWebViewService _mockWebViewService;

        [Header("UI")]
        [SerializeField] private PaymentProductListUI _productListUI;
        [SerializeField] private PaymentRewardGrantExample _rewardGrant;

        private PaymentConfiguration _configuration;
        private ILogger _logger;

        private async void Start()
        {
            PaymentSettings settings = PaymentSettings.Resolve(_settings);

            if (settings == null)
            {
                Debug.LogError(
                    "[PaymentBootstrapDirect] PaymentSettings is missing. Assign one in the inspector " +
                    "or create it via Tools > Game Payment SDK > Create Payment Settings."
                );
                return;
            }

            _logger = new Logger(Debug.unityLogger.logHandler)
            {
                logEnabled = settings.LogEnabled,
                filterLogType = settings.LogLevel
            };

            _configuration = settings.ToConfiguration();

            if (!_configuration.IsValid(out string configError))
            {
                Debug.LogError($"[PaymentBootstrapDirect] PaymentSettings is invalid: {configError}");
                return;
            }

            IPaymentWebViewService webViewService = ResolveWebViewService();

            if (webViewService == null)
            {
                Debug.LogError("[PaymentBootstrapDirect] WebView service is missing or invalid.");
                return;
            }

            webViewService.Logger = _logger;

            GamePayment.Initialized += HandleInitialized;
            GamePayment.ProductsUpdated += HandleProductsUpdated;
            GamePayment.PurchaseSucceeded += HandlePurchaseSucceeded;
            GamePayment.PurchaseFailed += HandlePurchaseFailed;

            PaymentResult<IReadOnlyCollection<PaymentProduct>> result =
                await GamePayment.InitializeAsync(
                    _configuration,
                    settings.PlayerId,
                    webViewService,
                    _logger
                );

            if (!result.Success)
            {
                Debug.LogWarning(
                    $"[PaymentBootstrapDirect] Initialize failed. reason={result.FailureReason}, error={result.ErrorMessage}"
                );
            }
        }

        public async void BuyProduct(string productKey)
        {
            if (GamePayment.IsPurchaseInProgress)
            {
                Debug.LogWarning("[PaymentBootstrapDirect] Purchase already in progress.");
                return;
            }

            if (_productListUI != null)
                _productListUI.SetInteractable(false);

            PaymentResult<List<PaymentPurchaseResult>> result =
                await GamePayment.PurchaseAsync(productKey);

            if (_productListUI != null)
                _productListUI.SetInteractable(true);

            if (!result.Success)
            {
                Debug.LogWarning(
                    $"[PaymentBootstrapDirect] Buy failed. productKey={productKey}, reason={result.FailureReason}, error={result.ErrorMessage}"
                );
            }
        }

        public async void ClaimPendingPurchases()
        {
            PaymentResult<List<PaymentPurchaseResult>> result =
                await GamePayment.ClaimPendingPurchasesAsync();

            if (!result.Success)
            {
                Debug.LogWarning(
                    $"[PaymentBootstrapDirect] Claim pending failed. reason={result.FailureReason}, error={result.ErrorMessage}"
                );
            }
        }

        private IPaymentWebViewService ResolveWebViewService()
        {
#if UNITY_EDITOR
            if (_mockWebViewService != null)
            {
                _mockWebViewService.SetConfiguration(_configuration);
                _mockWebViewService.Logger = _logger;
                return _mockWebViewService;
            }
#endif

            if (_deviceWebViewService is IPaymentWebViewService service)
                return service;

            return null;
        }

        private void HandleInitialized(PaymentInitializedEventArgs args)
        {
            Debug.Log($"[PaymentBootstrapDirect] Initialized: {args.Success}");

            if (!args.Success)
            {
                Debug.LogWarning(
                    $"[PaymentBootstrapDirect] Init failed. reason={args.FailureReason}, error={args.ErrorMessage}"
                );
            }
        }

        private void HandleProductsUpdated(IReadOnlyCollection<PaymentProduct> products)
        {
            Debug.Log($"[PaymentBootstrapDirect] Products loaded: {products.Count}");

            if (_productListUI != null)
                _productListUI.BindProducts(products, BuyProduct);
        }

        private void HandlePurchaseSucceeded(PaymentPurchaseResult result)
        {
            Debug.Log(
                $"[PaymentBootstrapDirect] Purchase succeeded. productKey={result.ProductKey}, orderId={result.OrderId}"
            );

            try
            {
                if (_rewardGrant != null)
                    _rewardGrant.Grant(result.ProductKey);

                GamePayment.ConfirmPurchaseProcessed(result);

                Debug.Log(
                    $"[PaymentBootstrapDirect] Purchase processed locally. transactionId={result.TransactionId}"
                );
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[PaymentBootstrapDirect] Reward grant failed. Purchase will remain unprocessed for recovery. error={exception.Message}"
                );
            }
        }

        private void HandlePurchaseFailed(PaymentPurchaseFailedEventArgs args)
        {
            Debug.LogWarning(
                $"[PaymentBootstrapDirect] Purchase failed. productKey={args.ProductKey}, reason={args.FailureReason}, error={args.ErrorMessage}"
            );
        }

        private void OnDestroy()
        {
            GamePayment.Initialized -= HandleInitialized;
            GamePayment.ProductsUpdated -= HandleProductsUpdated;
            GamePayment.PurchaseSucceeded -= HandlePurchaseSucceeded;
            GamePayment.PurchaseFailed -= HandlePurchaseFailed;

            GamePayment.Dispose();
        }
    }
}
