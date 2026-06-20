using System.Collections.Generic;
using GamePaymentSDK.Core;
using GamePaymentSDK.Direct;
using GamePaymentSDK.WebView;
using UnityEngine;

public sealed class PaymentBootstrap : MonoBehaviour
{
    [Header("Payment Settings")]
    [Tooltip("Payment data asset. If left empty, loads Resources/" + PaymentSettings.DefaultResourcePath + ".")]
    [SerializeField] private PaymentSettings _settings;

    [Header("WebView")]
    [SerializeField] private MonoBehaviour _webViewServiceComponent;

    private ILogger _logger;

    private async void Start()
    {
        PaymentSettings settings = PaymentSettings.Resolve(_settings);

        if (settings == null)
        {
            Debug.LogError(
                "[PaymentBootstrap] PaymentSettings is missing. Assign one in the inspector " +
                "or create it via Tools > Game Payment SDK > Create Payment Settings."
            );
            return;
        }

        if (!settings.IsValid(out string configError))
        {
            Debug.LogError($"[PaymentBootstrap] PaymentSettings is invalid: {configError}");
            return;
        }

        _logger = new Logger(Debug.unityLogger.logHandler)
        {
            logEnabled = settings.LogEnabled,
            filterLogType = settings.LogLevel
        };

        if (_webViewServiceComponent is not IPaymentWebViewService webViewService)
        {
            _logger.Log(LogType.Error, $"[PaymentSdk] [PaymentBootstrap] {nameof(_webViewServiceComponent)} must implement {nameof(IPaymentWebViewService)}");
            return;
        }

        webViewService.Logger = _logger;

        GamePayment.Initialized += OnInitialized;
        GamePayment.ProductsUpdated += OnProductsUpdated;
        GamePayment.PurchaseSucceeded += OnPurchaseSucceeded;
        GamePayment.PurchaseFailed += OnPurchaseFailed;

        PaymentResult<IReadOnlyCollection<PaymentProduct>> result =
            await GamePayment.InitializeAsync(settings, webViewService, _logger);

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
