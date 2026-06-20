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
    private IPaymentWebViewService _webViewService;
    private bool _ready;

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

        _webViewService = webViewService;
        _ready = true;
    }

    /// <summary>
    /// Call this (e.g. via UnityEvent) once the player identity is known.
    /// Triggers SDK initialization with the resolved player ID.
    /// </summary>
    public async void Initialize(string playerId)
    {
        if (!_ready)
        {
            Debug.LogError("[PaymentBootstrap] Cannot initialize: setup failed. Check earlier errors.");
            return;
        }

        _webViewService.Logger = _logger;

        GamePayment.Initialized += OnInitialized;
        GamePayment.ProductsUpdated += OnProductsUpdated;
        GamePayment.PurchaseSucceeded += OnPurchaseSucceeded;
        GamePayment.PurchaseFailed += OnPurchaseFailed;

        PaymentResult<IReadOnlyCollection<PaymentProduct>> result =
            await GamePayment.InitializeAsync(_settings, playerId, _webViewService, _logger);

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
