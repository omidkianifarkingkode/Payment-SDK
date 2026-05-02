using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public interface IPaymentPurchaseFlowService
    {
        bool IsPurchaseInProgress { get; }

        Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(
            string playerId,
            string productKey
        );
    }
}