/// Works Flow After WebView Callback:
/// 1. Callback URL detected
/// 2. SDK closes WebView
/// 3. PaymentClaimService.ClaimOrderAsync(playerId, orderId)
/// 4. POST /v1/payments/claim
/// 5-a If backend returns claimed item:
///     a.1. Convert to PaymentPurchaseResult
///     a.2. Remove local pending order
///     a.3. Notify game purchase succeeded
/// 5-b. If backend returns nothing:
///     b.1. Keep local pending order
///     b.2. Retry later
/// 
/// Note: Empty claim result is not treated as fatal.
/// Because this can happen when:
/// * Callback detected but backend verification is still processing
/// * Network returned before order became verified
/// * Gateway callback timing is inconsistent


using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public interface IPaymentClaimService
    {
        Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimOrderAsync(
            string playerId,
            string orderId
        );

        Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimAllForPlayerAsync(
            string playerId
        );

        Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimLocalPendingOrdersAsync(
            string playerId
        );
    }
}