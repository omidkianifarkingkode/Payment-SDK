using GamePaymentSDK.Core;
using GamePaymentSDK.UnityIAP;
using GamePaymentSDK.WebView;
using UnityEngine;

/// <summary>
/// Attach to a persistent GameObject (e.g. DontDestroyOnLoad) alongside the WebView component.
///
/// Typical usage:
///   1. Assign either <c>UniWebViewPaymentService</c> (device) or
///      <c>MockPaymentWebViewService</c> (Editor) to <c>_webViewServiceComponent</c>.
///   2. This bootstrap runs in Start — no other call required.
///   3. When the player identity is known, your code calls:
///      <code>
///      GamePaymentIapModule module = GamePaymentIap.CreateModule(playerId);
///      ConfigurationBuilder builder = ConfigurationBuilder.Instance(module, StandardPurchasingModule.Instance());
///      // builder.AddProduct(...)
///      UnityPurchasing.Initialize(storeListener, builder);
///      </code>
/// </summary>
public sealed class PaymentIapBootstrap : MonoBehaviour
{
    [Header("WebView")]
    [Tooltip("Assign UniWebViewPaymentService for device builds, or MockPaymentWebViewService for Editor testing.")]
    [SerializeField] private MonoBehaviour _webViewServiceComponent;

    private IPaymentSettings _settings;

    private void Start()
    {
        _settings = PaymentSettings.Load();

        if (_settings == null)
        {
            Debug.LogError("[PaymentIapBootstrap] PaymentSettings asset not found. Create one via Tools > Game Payment SDK > Create Payment Settings.");
            return;
        }

        if (!_settings.IsValid(out string configError))
        {
            Debug.LogError($"[PaymentIapBootstrap] PaymentSettings is invalid: {configError}");
            return;
        }

        ILogger logger = new Logger(Debug.unityLogger.logHandler)
        {
            logEnabled = _settings.LogEnabled,
            filterLogType = _settings.LogLevel
        };

        if (_webViewServiceComponent is not IPaymentWebViewService webViewService)
        {
            logger.Log(LogType.Error, $"[PaymentSdk] [PaymentIapBootstrap] {nameof(_webViewServiceComponent)} must implement {nameof(IPaymentWebViewService)}.");
            return;
        }

        GamePaymentIap.Setup(_settings, webViewService, logger);
    }

    private void OnDestroy()
    {
        PaymentSettings.Dispose();
        _settings = null;
    }
}
