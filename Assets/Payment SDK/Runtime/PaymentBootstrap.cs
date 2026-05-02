using System.Collections.Generic;
using GamePaymentSDK.Core;
using GamePaymentSDK.Direct;
using GamePaymentSDK.WebView;
using UnityEngine;

public sealed class PaymentBootstrap : MonoBehaviour
{
    [Header("Payment Config")]
    [SerializeField] private string _baseUrl;
    [SerializeField] private string _apiKey;
    [SerializeField] private string _clientId;
    [SerializeField] private string _playerId;

    [Header("WebView")]
    [SerializeField] private MonoBehaviour _webViewServiceComponent;

    private void Awake()
    {
        var webViewService = _webViewServiceComponent as IPaymentWebViewService;

        if (webViewService == null)
        {
            Debug.LogError(
                $"{nameof(_webViewServiceComponent)} must implement {nameof(IPaymentWebViewService)}",
                this
            );
        }
    }

    private async void Start()
    {
        PaymentConfiguration config = new PaymentConfiguration
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

        GamePayment.Initialized += OnInitialized;
        GamePayment.ProductsUpdated += OnProductsUpdated;
        GamePayment.PurchaseSucceeded += OnPurchaseSucceeded;
        GamePayment.PurchaseFailed += OnPurchaseFailed;

        PaymentResult<IReadOnlyCollection<PaymentProduct>> result =
            await GamePayment.InitializeAsync(
                config,
                _playerId,
                _webViewServiceComponent as IPaymentWebViewService
            );

        if (!result.Success)
        {
            Debug.LogWarning($"Payment init failed: {result.FailureReason} / {result.ErrorMessage}");
        }
    }

    public async void BuySmallGemPack()
    {
        await GamePayment.PurchaseAsync("gem_pack_small");
    }

    private void OnInitialized(PaymentInitializedEventArgs args)
    {
        Debug.Log($"Payment initialized: {args.Success}");
    }

    private void OnProductsUpdated(IReadOnlyCollection<PaymentProduct> products)
    {
        Debug.Log($"Payment products loaded: {products.Count}");
    }

    private void OnPurchaseSucceeded(PaymentPurchaseResult result)
    {
        Debug.Log($"Purchase succeeded. product={result.ProductKey}, order={result.OrderId}");

        // Grant reward here.
        // Example:
        // RewardManager.Grant(result.ProductKey);
    }

    private void OnPurchaseFailed(PaymentPurchaseFailedEventArgs args)
    {
        Debug.LogWarning(
            $"Purchase failed. product={args.ProductKey}, reason={args.FailureReason}, error={args.ErrorMessage}"
        );
    }

    private void OnDestroy()
    {
        GamePayment.Initialized -= OnInitialized;
        GamePayment.ProductsUpdated -= OnProductsUpdated;
        GamePayment.PurchaseSucceeded -= OnPurchaseSucceeded;
        GamePayment.PurchaseFailed -= OnPurchaseFailed;

        GamePayment.Dispose();
    }
}