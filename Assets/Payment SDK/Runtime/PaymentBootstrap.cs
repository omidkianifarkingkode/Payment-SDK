using System.Collections.Generic;
using GamePaymentSDK.Core;
using GamePaymentSDK.Direct;
using GamePaymentSDK.WebView;
using UnityEngine;

public sealed class PaymentBootstrap : MonoBehaviour
{
    [Header("WebView")]
    [SerializeField] private MonoBehaviour _webViewServiceComponent;

    private PaymentSettings _settings;
    private ILogger _logger;

    private void Start()
    {
        _settings = PaymentSettings.Load();

        if (_settings == null)
        {
            Debug.LogError("[PaymentBootstrap] PaymentSettings asset not found. Create one via Tools > Game Payment SDK > Create Payment Settings.");
            return;
        }

        if (!_settings.IsValid(out string configError))
        {
            Debug.LogError($"[PaymentBootstrap] PaymentSettings is invalid: {configError}");
            return;
        }

        _logger = new Logger(Debug.unityLogger.logHandler)
        {
            logEnabled = _settings.LogEnabled,
            filterLogType = _settings.LogLevel
        };

        if (_webViewServiceComponent is not IPaymentWebViewService webViewService)
        {
            _logger.Log(LogType.Error, $"[PaymentSdk] [PaymentBootstrap] {nameof(_webViewServiceComponent)} must implement {nameof(IPaymentWebViewService)}");
            return;
        }

        GamePayment.Initialized += OnInitialized;
        GamePayment.ProductsUpdated += OnProductsUpdated;
        GamePayment.PurchaseSucceeded += OnPurchaseSucceeded;
        GamePayment.PurchaseFailed += OnPurchaseFailed;

        GamePayment.Setup(_settings, webViewService, _logger);
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
    }

    private void OnPurchaseFailed(PaymentPurchaseFailedEventArgs args)
    {
        _logger.Log(LogType.Warning, $"[PaymentSdk] [PaymentBootstrap] Purchase failed. product={args.ProductKey}, reason={args.FailureReason}, error={args.ErrorMessage}");
    }

    private void OnDestroy()
    {
        GamePayment.Initialized -= OnInitialized;
        GamePayment.ProductsUpdated -= OnProductsUpdated;
        GamePayment.PurchaseSucceeded -= OnPurchaseSucceeded;
        GamePayment.PurchaseFailed -= OnPurchaseFailed;

        GamePayment.Dispose();
        _settings?.Dispose();
    }
}
