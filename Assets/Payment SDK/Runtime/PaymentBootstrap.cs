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

    [Header("Logging")]
    [SerializeField] private bool _logEnabled = true;
    [SerializeField] private LogType _logLevel = LogType.Log;

    private ILogger _logger;

    private async void Start()
    {
        _logger = new Logger(Debug.unityLogger.logHandler)
        {
            logEnabled = _logEnabled,
            filterLogType = _logLevel
        };

        PaymentConfiguration config = new()
        {
            BaseUrl = _baseUrl,
            ApiKey = _apiKey,
            Environment = PaymentEnvironment.Production,
            RequestTimeoutSeconds = 20,
            WebViewTimeoutSeconds = 300,
            ClaimRetryCount = 3
        };

        GamePayment.Initialized += OnInitialized;
        GamePayment.ProductsUpdated += OnProductsUpdated;
        GamePayment.PurchaseSucceeded += OnPurchaseSucceeded;
        GamePayment.PurchaseFailed += OnPurchaseFailed;

        if (_webViewServiceComponent is not IPaymentWebViewService webViewService)
        {
            _logger.Log(LogType.Error, $"[PaymentSdk] [PaymentBootstrap] {nameof(_webViewServiceComponent)} must implement {nameof(IPaymentWebViewService)}");
            return;
        }

        webViewService.Logger = _logger;

        PaymentResult<IReadOnlyCollection<PaymentProduct>> result =
            await GamePayment.InitializeAsync(
                config,
                _playerId,
                webViewService,
                _logger
            );

        if (!result.Success)
        {
            _logger.Log(LogType.Warning, $"[PaymentSdk] [PaymentBootstrap] Payment init failed: {result.FailureReason} / {result.ErrorMessage}");
        }
    }

    public async void BuySmallGemPack()
    {
        await GamePayment.PurchaseAsync("gem_pack_small");
    }

    private void OnInitialized(PaymentInitializedEventArgs args)
    {
        _logger.Log(LogType.Log, $"[PaymentSdk] [PaymentBootstrap] Payment initialized: {args.Success}");
    }

    private void OnProductsUpdated(IReadOnlyCollection<PaymentProduct> products)
    {
        _logger.Log(LogType.Log, $"[PaymentSdk] [PaymentBootstrap] Payment products loaded: {products.Count}");
    }

    private void OnPurchaseSucceeded(PaymentPurchaseResult result)
    {
        _logger.Log(LogType.Log, $"[PaymentSdk] [PaymentBootstrap] Purchase succeeded. product={result.ProductKey}, order={result.OrderId}");

        // Grant reward here.
        // Example:
        // RewardManager.Grant(result.ProductKey);
    }

    private void OnPurchaseFailed(PaymentPurchaseFailedEventArgs args)
    {
        _logger.Log(LogType.Warning, $"[PaymentSdk] [PaymentBootstrap] Purchase failed." +
            $" product={args.ProductKey}, reason={args.FailureReason}, error={args.ErrorMessage}");
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
