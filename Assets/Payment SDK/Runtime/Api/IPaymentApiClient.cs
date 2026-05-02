using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Api
{
    public interface IPaymentApiClient
    {
        Task<PaymentResult<List<PaymentProduct>>> GetProductsAsync();

        Task<PaymentResult<PaymentRequestResponseDto>> RequestPaymentAsync(
            string playerId,
            string productKey
        );

        Task<PaymentResult<List<ClaimItemDto>>> ClaimAsync(
            string playerId,
            string orderId = null
        );
    }
}