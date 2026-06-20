using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GamePaymentSDK.Direct
{
    public static class GamePayment
    {
        public static event Action<PaymentInitializedEventArgs> Initialized;
        public static event Action<IReadOnlyCollection<PaymentProduct>> ProductsUpdated;
        public static event Action<PaymentPurchaseResult> PurchaseSucceeded;
        public static event Action<PaymentPurchaseFailedEventArgs> PurchaseFailed;

        private static GamePaymentController _controller;
        private static ILogger _logger;

        private static PaymentSettings _settings;
        private static IPaymentWebViewService _webViewService;

        public static bool IsInitialized =>
            _controller != null &&
            _controller.IsInitialized;

        public static bool IsInitializing =>
            _controller != null &&
            _controller.IsInitializing;

        public static bool IsPurchaseInProgress =>
            _controller != null &&
            _controller.IsPurchaseInProgress;

        public static string PlayerId =>
            _controller?.PlayerId;

        /// <summary>
        /// Stores the SDK configuration. Call this once from your bootstrap (e.g. in Start).
        /// Afterwards call <see cref="InitializeAsync"/> when the player identity is known.
        /// </summary>
        internal static void Setup(
            PaymentSettings settings,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            _settings = settings;
            _webViewService = webViewService;
            _logger = logger;

            if (_webViewService != null)
                _webViewService.Logger = logger;
        }

        /// <summary>
        /// Initializes the SDK with the resolved player identity.
        /// Requires <see cref="Setup"/> to have been called first.
        /// </summary>
        public static async Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> InitializeAsync(string playerId)
        {
            if (_settings == null)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "Call GamePayment.Setup before InitializeAsync."
                );
            }

            Dispose();

            _controller = new GamePaymentController(
                _settings,
                playerId,
                _webViewService,
                _logger
            );

            HookControllerEvents(_controller);

            return await _controller.InitializeAsync();
        }

        public static IReadOnlyCollection<PaymentProduct> GetProducts()
        {
            if (_controller == null)
                return Array.Empty<PaymentProduct>();

            return _controller.GetProducts();
        }

        public static bool TryGetProduct(string productKey, out PaymentProduct product)
        {
            product = null;

            if (_controller == null)
                return false;

            return _controller.TryGetProduct(productKey, out product);
        }

        public static Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(string productKey)
        {
            if (_controller == null)
            {
                return Task.FromResult(
                    PaymentResult<List<PaymentPurchaseResult>>.Fail(
                        PaymentFailureReason.NotInitialized,
                        "GamePayment is not initialized."
                    )
                );
            }

            return _controller.PurchaseAsync(productKey);
        }

        public static Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimPendingPurchasesAsync()
        {
            if (_controller == null)
            {
                return Task.FromResult(
                    PaymentResult<List<PaymentPurchaseResult>>.Fail(
                        PaymentFailureReason.NotInitialized,
                        "GamePayment is not initialized."
                    )
                );
            }

            return _controller.ClaimPendingPurchasesAsync();
        }

        public static void ConfirmPurchaseProcessed(PaymentPurchaseResult purchase)
        {
            if (_controller == null)
            {
                _logger?.Log(LogType.Warning, "[PaymentSdk] [GamePayment] Cannot confirm purchase because GamePayment is not initialized.");
                return;
            }

            _controller.ConfirmPurchaseProcessed(purchase);
        }

        public static void Dispose()
        {
            if (_controller != null)
            {
                UnhookControllerEvents(_controller);
                _controller.Dispose();
                _controller = null;
            }
        }

        private static void HookControllerEvents(GamePaymentController controller)
        {
            controller.Initialized += OnControllerInitialized;
            controller.ProductsUpdated += OnControllerProductsUpdated;
            controller.PurchaseSucceeded += OnControllerPurchaseSucceeded;
            controller.PurchaseFailed += OnControllerPurchaseFailed;
        }

        private static void UnhookControllerEvents(GamePaymentController controller)
        {
            controller.Initialized -= OnControllerInitialized;
            controller.ProductsUpdated -= OnControllerProductsUpdated;
            controller.PurchaseSucceeded -= OnControllerPurchaseSucceeded;
            controller.PurchaseFailed -= OnControllerPurchaseFailed;
        }

        private static void OnControllerInitialized(PaymentInitializedEventArgs args) =>
            Initialized?.Invoke(args);

        private static void OnControllerProductsUpdated(IReadOnlyCollection<PaymentProduct> products) =>
            ProductsUpdated?.Invoke(products);

        private static void OnControllerPurchaseSucceeded(PaymentPurchaseResult result) =>
            PurchaseSucceeded?.Invoke(result);

        private static void OnControllerPurchaseFailed(PaymentPurchaseFailedEventArgs args) =>
            PurchaseFailed?.Invoke(args);
    }
}
