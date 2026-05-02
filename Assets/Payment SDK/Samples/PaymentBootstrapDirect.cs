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
        [SerializeField] private string _baseUrl;
        [SerializeField] private string _apiKey;
        [SerializeField] private string _clientId;
        [SerializeField] private string _playerId;

        [Header("WebView")]
        [Tooltip("Assign UniWebViewPaymentService here for device builds.")]
        [SerializeField] private MonoBehaviour _deviceWebViewService;

        [Tooltip("Assign MockPaymentWebViewService here for Editor testing.")]
        [SerializeField] private MockPaymentWebViewService _mockWebViewService;

        [Header("UI")]
        [SerializeField] private PaymentProductListUI _productListUI;
        [SerializeField] private PaymentRewardGrantExample _rewardGrant;

        private PaymentConfiguration _configuration;

        private async void Start()
        {
            _configuration = new PaymentConfiguration
            {
                BaseUrl = _baseUrl,
                ApiKey = _apiKey,
                ClientId = _clientId,
                Environment = PaymentEnvironment.Production,
                RequestTimeoutSeconds = 20,
                WebViewTimeoutSeconds = 300,
                ClaimRetryCount = 3,
                EnableLogs = true
            };

            IPaymentWebViewService webViewService = ResolveWebViewService();

            if (webViewService == null)
            {
                Debug.LogError("[PaymentBootstrapDirect] WebView service is missing or invalid.");
                return;
            }

            GamePayment.Initialized += HandleInitialized;
            GamePayment.ProductsUpdated += HandleProductsUpdated;
            GamePayment.PurchaseSucceeded += HandlePurchaseSucceeded;
            GamePayment.PurchaseFailed += HandlePurchaseFailed;

            PaymentResult<IReadOnlyCollection<PaymentProduct>> result =
                await GamePayment.InitializeAsync(
                    _configuration,
                    _playerId,
                    webViewService
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