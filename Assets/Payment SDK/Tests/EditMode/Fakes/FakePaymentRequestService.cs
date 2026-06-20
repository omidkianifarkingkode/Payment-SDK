using System.Threading.Tasks;
using GamePaymentSDK.Core;
using GamePaymentSDK.Services;

namespace GamePaymentSDK.Tests.EditMode.Fakes
{
    /// <summary>
    /// Stub <see cref="IPaymentRequestService"/> that returns a preconfigured
    /// start result and records the lifecycle marker calls the flow makes.
    /// </summary>
    public sealed class FakePaymentRequestService : IPaymentRequestService
    {
        public PaymentResult<PaymentStartResult> RequestResult { get; set; }

        public bool IsPurchaseInProgress { get; set; }
        public string ActiveOrderId { get; set; }

        public int ClearActivePurchaseCallCount { get; private set; }
        public bool WebViewOpenedMarked { get; private set; }
        public bool CallbackDetectedMarked { get; private set; }
        public bool PurchaseFailedMarked { get; private set; }

        public Task<PaymentResult<PaymentStartResult>> RequestPaymentAsync(
            string playerId,
            string productKey
        )
        {
            return Task.FromResult(RequestResult);
        }

        public void ClearActivePurchase() => ClearActivePurchaseCallCount++;

        public void MarkWebViewOpened(string orderId) => WebViewOpenedMarked = true;

        public void MarkCallbackDetected(string orderId) => CallbackDetectedMarked = true;

        public void MarkPurchaseFailed(string orderId) => PurchaseFailedMarked = true;
    }
}
