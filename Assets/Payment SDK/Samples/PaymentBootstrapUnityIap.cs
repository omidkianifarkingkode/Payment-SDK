#if GAME_PAYMENT_UNITY_IAP

using GamePaymentSDK.Core;
using GamePaymentSDK.UnityIAP;
using GamePaymentSDK.WebView;
using GamePaymentSDK.WebView.Mock;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace GamePaymentSDK.Samples
{
    public sealed class PaymentBootstrapUnityIap : MonoBehaviour, IDetailedStoreListener
    {
        [Header("Payment Settings")]
        [Tooltip("Payment data asset. If left empty, loads Resources/" + PaymentSettings.DefaultResourcePath + ".")]
        [SerializeField] private PaymentSettings _settings;

        [Header("Products")]
        [SerializeField] private string[] _productKeys =
        {
            "gem_pack_small",
            "gem_pack_medium"
        };

        [Header("WebView")]
        [SerializeField] private MonoBehaviour _deviceWebViewService;
        [SerializeField] private MockPaymentWebViewService _mockWebViewService;

        [Header("Reward")]
        [SerializeField] private PaymentRewardGrantExample _rewardGrant;

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        private ILogger _logger;

        private void Start()
        {
            InitializePurchasing();
        }

        public void BuyProduct(string productKey)
        {
            if (_storeController == null)
            {
                _logger?.Log(LogType.Error, "[PaymentSdk] [PaymentBootstrapUnityIap] Store controller is not ready.");
                return;
            }

            Product product = _storeController.products.WithID(productKey);

            if (product == null)
            {
                _logger?.Log(LogType.Error, $"[PaymentSdk] [PaymentBootstrapUnityIap] Product not found: {productKey}");
                return;
            }

            if (!product.availableToPurchase)
            {
                _logger?.Log(LogType.Error, $"[PaymentSdk] [PaymentBootstrapUnityIap] Product is not available: {productKey}");
                return;
            }

            _storeController.InitiatePurchase(product);
        }

        private void InitializePurchasing()
        {
            PaymentSettings settings = PaymentSettings.Resolve(_settings);

            if (settings == null)
            {
                Debug.LogError(
                    "[PaymentBootstrapUnityIap] PaymentSettings is missing. Assign one in the inspector " +
                    "or create it via Tools > Game Payment SDK > Create Payment Settings."
                );
                return;
            }

            if (!settings.IsValid(out string configError))
            {
                Debug.LogError($"[PaymentBootstrapUnityIap] PaymentSettings is invalid: {configError}");
                return;
            }

            _logger = new Logger(Debug.unityLogger.logHandler)
            {
                logEnabled = settings.LogEnabled,
                filterLogType = settings.LogLevel
            };

            IPaymentWebViewService webViewService = ResolveWebViewService(settings);

            if (webViewService == null)
            {
                _logger.Log(LogType.Error, "[PaymentSdk] [PaymentBootstrapUnityIap] WebView service is missing or invalid.");
                return;
            }

            webViewService.Logger = _logger;

            GamePaymentIapModule paymentModule = GamePaymentIapModule.Instance(
                settings,
                webViewService,
                _logger
            );

            ConfigurationBuilder builder = ConfigurationBuilder.Instance(
                paymentModule,
                StandardPurchasingModule.Instance()
            );

            foreach (string productKey in _productKeys)
            {
                if (string.IsNullOrWhiteSpace(productKey))
                    continue;

                IDs ids = new IDs
                {
                    { productKey, GamePaymentIapStoreConstants.StoreName }
                };

                builder.AddProduct(
                    productKey,
                    ProductType.Consumable,
                    ids
                );
            }

            UnityPurchasing.Initialize(this, builder);
        }

        private IPaymentWebViewService ResolveWebViewService(PaymentSettings settings)
        {
#if UNITY_EDITOR
            if (_mockWebViewService != null)
            {
                _mockWebViewService.SetSettings(settings);
                _mockWebViewService.Logger = _logger;
                return _mockWebViewService;
            }
#endif

            if (_deviceWebViewService is IPaymentWebViewService service)
                return service;

            return null;
        }

        public void OnInitialized(
            IStoreController controller,
            IExtensionProvider extensions
        )
        {
            _storeController = controller;
            _extensionProvider = extensions;

            Debug.Log("[PaymentBootstrapUnityIap] Unity IAP initialized.");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning($"[PaymentBootstrapUnityIap] Initialize failed: {error}");
        }

        public void OnInitializeFailed(
            InitializationFailureReason error,
            string message
        )
        {
            Debug.LogWarning(
                $"[PaymentBootstrapUnityIap] Initialize failed: {error}, message={message}"
            );
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Product product = args.purchasedProduct;

            Debug.Log(
                $"[PaymentBootstrapUnityIap] ProcessPurchase. product={product.definition.id}, transactionId={product.transactionID}, receipt={product.receipt}"
            );

            if (_rewardGrant != null)
                _rewardGrant.Grant(product.definition.id);

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(
            Product product,
            UnityEngine.Purchasing.PurchaseFailureReason failureReason
        )
        {
            Debug.LogWarning(
                $"[PaymentBootstrapUnityIap] Purchase failed. product={product?.definition?.id}, reason={failureReason}"
            );
        }

        public void OnPurchaseFailed(
            Product product,
            PurchaseFailureDescription failureDescription
        )
        {
            Debug.LogWarning(
                $"[PaymentBootstrapUnityIap] Purchase failed. product={product?.definition?.id}, reason={failureDescription.reason}, message={failureDescription.message}"
            );
        }
    }
}

#endif
