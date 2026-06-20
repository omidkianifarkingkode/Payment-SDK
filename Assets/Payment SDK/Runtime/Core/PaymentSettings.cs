using UnityEngine;

namespace GamePaymentSDK.Core
{
    /// <summary>
    /// Designer-facing payment configuration stored as a ScriptableObject asset.
    ///
    /// Holds DATA only (server, identity, timeouts). Scene/component references
    /// (WebView services, UI) are intentionally NOT stored here: a ScriptableObject
    /// asset cannot serialize references to scene objects, so those stay on the
    /// MonoBehaviour bootstrap.
    ///
    /// Call <see cref="ToConfiguration"/> to get the runtime <see cref="PaymentConfiguration"/>
    /// so the SDK core stays dependent on PaymentConfiguration, not on this asset.
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

        [Tooltip("Stable player identifier used for purchase/claim.")]
        [SerializeField] private string _playerId;

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
        [SerializeField] private bool _enableLogs = true;

        public string BaseUrl => _baseUrl;
        public string ApiKey => _apiKey;
        public string PlayerId => _playerId;
        public PaymentEnvironment Environment => _environment;

        /// <summary>Builds the runtime configuration consumed by the SDK.</summary>
        public PaymentConfiguration ToConfiguration()
        {
            return new PaymentConfiguration
            {
                BaseUrl = _baseUrl,
                ApiKey = _apiKey,
                Environment = _environment,
                RequestTimeoutSeconds = _requestTimeoutSeconds,
                WebViewTimeoutSeconds = _webViewTimeoutSeconds,
                ClaimRetryCount = _claimRetryCount,
                PendingOrderCleanupDays = _pendingOrderCleanupDays,
                ProcessedTransactionHistoryDays = _processedTransactionHistoryDays,
                EnableLogs = _enableLogs
            };
        }

        /// <summary>
        /// Loads the default <see cref="PaymentSettings"/> asset from
        /// Resources/GamePayment/PaymentSettings, or null if none exists.
        /// </summary>
        public static PaymentSettings LoadDefault()
        {
            return Resources.Load<PaymentSettings>(DefaultResourcePath);
        }

        // ---- Resolver: load the default asset once and cache it ----

        private static PaymentSettings _instance;

        /// <summary>
        /// The cached default settings instance. Loaded from Resources on first
        /// access and reused afterwards (the static cache is cleared automatically
        /// on domain reload / play-mode restart). Returns null if no asset exists.
        /// </summary>
        public static PaymentSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = LoadDefault();

                return _instance;
            }
        }

        /// <summary>
        /// Resolves the active settings: prefers an explicitly provided asset,
        /// otherwise falls back to the cached default <see cref="Instance"/>.
        /// </summary>
        public static PaymentSettings Resolve(PaymentSettings preferred = null)
        {
            if (preferred != null)
            {
                _instance = preferred;
                return preferred;
            }

            return Instance;
        }

        private void OnValidate()
        {
            _baseUrl = _baseUrl?.Trim();
            _apiKey = _apiKey?.Trim();
            _playerId = _playerId?.Trim();

            if (string.IsNullOrWhiteSpace(_baseUrl))
                Debug.LogWarning($"[PaymentSettings] BaseUrl is empty on '{name}'.", this);

            if (string.IsNullOrWhiteSpace(_apiKey))
                Debug.LogWarning($"[PaymentSettings] ApiKey is empty on '{name}'.", this);

            if (string.IsNullOrWhiteSpace(_playerId))
                Debug.LogWarning($"[PaymentSettings] PlayerId is empty on '{name}'.", this);
        }
    }
}
