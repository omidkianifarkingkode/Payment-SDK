using System.Threading.Tasks;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public interface IPaymentRequestService
    {
        bool IsPurchaseInProgress { get; }
        string ActiveOrderId { get; }

        Task<PaymentResult<PaymentStartResult>> RequestPaymentAsync(
            string playerId,
            string productKey
        );

        void ClearActivePurchase();

        void MarkWebViewOpened(string orderId);

        void MarkCallbackDetected(string orderId);

        void MarkPurchaseFailed(string orderId);
    }
}