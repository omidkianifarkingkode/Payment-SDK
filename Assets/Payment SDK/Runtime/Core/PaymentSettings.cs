using System;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GamePaymentSDK.Core
{
    public interface IPaymentSettings 
    {
        public string ApiKey { get; }

        public string BaseUrl { get; }

        public string ProductsUrl { get; }
        public string RequestUrl { get; }
        public string ClaimUrl { get; }

        public int RequestTimeoutSeconds { get; }
        public int WebViewTimeoutSeconds { get; }

        public int ClaimRetryCount { get; }
        public int PendingOrderCleanupDays { get; }
        public int ProcessedTransactionHistoryDays { get; }

        public bool LogEnabled { get; }
        public LogType LogLevel { get; }

        bool IsValid(out string error);
    }

    [CreateAssetMenu(fileName = "PaymentSettings", menuName = "Game Payment SDK/Payment Settings")]
    public sealed class PaymentSettings : ScriptableObject, IPaymentSettings
    {
        public const string DefaultResourcePath = "GamePayment/PaymentSettings";

        [Tooltip("Client API key. The server resolves the client from this key.")]
        [SerializeField] private string _apiKey;

        [Header("Environment")]
        [Tooltip("Selects which base URL is active at runtime. Production uses the Production URL; anything else uses the Staging URL.")]
        [SerializeField] private PaymentEnvironment _environment = PaymentEnvironment.Production;
        [Tooltip("Production host root. WITHOUT trailing /api or /v1. Example: https://payment.kingkodegames.ir")]
        [SerializeField] private string _productionBaseUrl;
        [Tooltip("Staging host root. WITHOUT trailing /api or /v1. Example: https://payment-staging.kingkodegames.ir")]
        [SerializeField] private string _stagingBaseUrl;

        [Header("Routes")]
        [SerializeField] private string _productRoute = "api/v1/products";
        [SerializeField] private string _requestRoute = "api/v1/payments/request";
        [SerializeField] private string _claimRoute = "api/v1/payments/claim";

        [Header("Timeouts (seconds)")]
        [Min(1)][SerializeField] private int _requestTimeoutSeconds = 20;
        [Min(1)][SerializeField] private int _webViewTimeoutSeconds = 300;

        [Header("Recovery / cleanup")]
        [Min(0)][SerializeField] private int _claimRetryCount = 3;
        [Min(0)][SerializeField] private int _pendingOrderCleanupDays = 7;
        [Min(0)][SerializeField] private int _processedTransactionHistoryDays = 90;

        [Header("Logging")]
        [SerializeField] private bool _logEnabled = true;
        [SerializeField] private LogType _logLevel = LogType.Log;

        public string ApiKey => _apiKey;

        public string BaseUrl
        {
            get
            {
                var url = _environment == PaymentEnvironment.Production ? _productionBaseUrl : _stagingBaseUrl;

                if (string.IsNullOrWhiteSpace(url))
                    return string.Empty;

                return url.TrimEnd('/');

                return url;
            }
        }

        public string ProductsUrl => BuildUrl(BaseUrl, _productRoute);
        public string RequestUrl => BuildUrl(BaseUrl, _requestRoute);
        public string ClaimUrl => BuildUrl(BaseUrl, _claimRoute);

        public int RequestTimeoutSeconds => _requestTimeoutSeconds;
        public int WebViewTimeoutSeconds => _webViewTimeoutSeconds;

        public int ClaimRetryCount => _claimRetryCount;
        public int PendingOrderCleanupDays => _pendingOrderCleanupDays;
        public int ProcessedTransactionHistoryDays => _processedTransactionHistoryDays;

        public bool LogEnabled => _logEnabled;
        public LogType LogLevel => _logLevel;

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                error = _environment == PaymentEnvironment.Production
                    ? "Production BaseUrl is required."
                    : "Staging BaseUrl is required.";
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

        private void OnValidate()
        {
            _productionBaseUrl = _productionBaseUrl?.Trim();
            _stagingBaseUrl = _stagingBaseUrl?.Trim();
            _apiKey = _apiKey?.Trim();

            if (string.IsNullOrWhiteSpace(_productionBaseUrl))
                Debug.LogWarning($"[PaymentSettings] Production BaseUrl is empty on '{name}'.", this);
            if (string.IsNullOrWhiteSpace(_stagingBaseUrl))
                Debug.LogWarning($"[PaymentSettings] Staging BaseUrl is empty on '{name}'.", this);
            if (string.IsNullOrWhiteSpace(_apiKey))
                Debug.LogWarning($"[PaymentSettings] ApiKey is empty on '{name}'.", this);
        }

        private static string BuildUrl(string baseUrl, string route)
        {
            if (string.IsNullOrEmpty(baseUrl))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(route))
                return baseUrl;

            return baseUrl + "/" + route.TrimStart('/');
        }

        private static PaymentSettings _instance;

        public static IPaymentSettings Load()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<PaymentSettings>(DefaultResourcePath);
            }
            return _instance;
        }

        public static void Dispose()
        {
            Resources.UnloadAsset(_instance);
            _instance = null;
        }
    }
}