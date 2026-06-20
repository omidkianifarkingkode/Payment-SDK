using System;
using UnityEngine;

namespace GamePaymentSDK.Core
{
    /// <summary>
    /// Payment configuration asset. Holds all runtime settings (server, identity, timeouts,
    /// logging). Scene/component references (WebView services, UI) are intentionally NOT
    /// stored here — those stay on the MonoBehaviour bootstrap.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PaymentSettings",
        menuName = "Game Payment SDK/Payment Settings",
        order = 0
    )]
    public sealed class PaymentSettings : ScriptableObject
    {
        /// <summary>Resources-relative path used by the runtime fallback loader.</summary>
        public const string DefaultResourcePath = "GamePayment/PaymentSettings";

        [Header("Payment Service")]
        [Tooltip("Base URL WITHOUT the trailing /v1. Example: https://payment-staging.kingkodegames.ir/api")]
        [SerializeField] private string _baseUrl;

        [Tooltip("Client API key. The server resolves the client from this key.")]
        [SerializeField] private string _apiKey;

        [Header("Environment")]
        [SerializeField] private PaymentEnvironment _environment = PaymentEnvironment.Production;

        [Header("Timeouts (seconds)")]
        [Min(1)] [SerializeField] private int _requestTimeoutSeconds = 20;
        [Min(1)] [SerializeField] private int _webViewTimeoutSeconds = 300;

        [Header("Recovery / cleanup")]
        [Min(0)] [SerializeField] private int _claimRetryCount = 3;
        [Min(0)] [SerializeField] private int _pendingOrderCleanupDays = 7;
        [Min(0)] [SerializeField] private int _processedTransactionHistoryDays = 90;

        [Header("Logging")]
        [SerializeField] private bool _logEnabled = true;
        [SerializeField] private LogType _logLevel = LogType.Log;

        public string BaseUrl => _baseUrl;
        public string ApiKey => _apiKey;
        public PaymentEnvironment Environment => _environment;
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;
        public int WebViewTimeoutSeconds => _webViewTimeoutSeconds;
        public int ClaimRetryCount => _claimRetryCount;
        public int PendingOrderCleanupDays => _pendingOrderCleanupDays;
        public int ProcessedTransactionHistoryDays => _processedTransactionHistoryDays;
        public bool LogEnabled => _logEnabled;
        public LogType LogLevel => _logLevel;

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                error = "BaseUrl is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                error = "ApiKey is required.";
                return false;
            }

            error = null;
            return true;
        }

        public string GetNormalizedBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return string.Empty;

            return _baseUrl.TrimEnd('/');
        }

        private static PaymentSettings _instance;

        public static PaymentSettings Load()
        {
            if (_instance == null) 
            {
                _instance = Resources.Load<PaymentSettings>(DefaultResourcePath);
            }

            return _instance;
        }

        private void OnValidate()
        {
            _baseUrl = _baseUrl?.Trim();
            _apiKey = _apiKey?.Trim();

            if (string.IsNullOrWhiteSpace(_baseUrl))
                Debug.LogWarning($"[PaymentSettings] BaseUrl is empty on '{name}'.", this);

            if (string.IsNullOrWhiteSpace(_apiKey))
                Debug.LogWarning($"[PaymentSettings] ApiKey is empty on '{name}'.", this);
        }

        public void Dispose()
        {
            Resources.UnloadAsset(_instance);
            _instance = null;
        }
    }
}
