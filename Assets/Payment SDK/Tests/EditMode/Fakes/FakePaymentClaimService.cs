using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;
using GamePaymentSDK.Services;

namespace GamePaymentSDK.Tests.EditMode.Fakes
{
    /// <summary>
    /// Stub <see cref="IPaymentClaimService"/> for purchase-flow tests. The
    /// claim service has its own dedicated tests; here we only need to control
    /// what a claim returns and count how often the flow claims.
    /// </summary>
    public sealed class FakePaymentClaimService : IPaymentClaimService
    {
        public PaymentResult<List<PaymentPurchaseResult>> ClaimOrderResult { get; set; }
        public PaymentResult<List<PaymentPurchaseResult>> ClaimAllResult { get; set; }

        public int ClaimOrderCallCount { get; private set; }

        public Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimOrderAsync(
            string playerId,
            string orderId
        )
        {
            ClaimOrderCallCount++;
            return Task.FromResult(ClaimOrderResult);
        }

        public Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimAllForPlayerAsync(
            string playerId
        )
        {
            return Task.FromResult(ClaimAllResult);
        }

        public Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimLocalPendingOrdersAsync(
            string playerId
        )
        {
            return Task.FromResult(ClaimAllResult);
        }
    }
}
