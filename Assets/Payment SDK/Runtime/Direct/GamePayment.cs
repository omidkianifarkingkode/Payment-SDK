using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;

namespace GamePaymentSDK.Direct
{
    public static class GamePayment
    {
        public static event Action<PaymentInitializedEventArgs> Initialized;
        public static event Action<IReadOnlyCollection<PaymentProduct>> ProductsUpdated;
        public static event Action<PaymentPurchaseResult> PurchaseSucceeded;
        public static event Action<PaymentPurchaseFailedEventArgs> PurchaseFailed;

        private static GamePaymentController _controller;

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

        public static async Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> InitializeAsync(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService
        )
        {
            Dispose();

            _controller = new GamePaymentController(
                configuration,
                playerId,
                webViewService
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

        public static Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(
            string productKey
        )
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
                PaymentLogger.LogWarning(
                    "Cannot confirm purchase because GamePayment is not initialized."
                );
        
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

        private static void OnControllerInitialized(PaymentInitializedEventArgs args)
        {
            Initialized?.Invoke(args);
        }

        private static void OnControllerProductsUpdated(
            IReadOnlyCollection<PaymentProduct> products
        )
        {
            ProductsUpdated?.Invoke(products);
        }

        private static void OnControllerPurchaseSucceeded(PaymentPurchaseResult result)
        {
            PurchaseSucceeded?.Invoke(result);
        }

        private static void OnControllerPurchaseFailed(PaymentPurchaseFailedEventArgs args)
        {
            PurchaseFailed?.Invoke(args);
        }
    }
}