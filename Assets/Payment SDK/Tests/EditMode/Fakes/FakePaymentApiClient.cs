using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Tests.EditMode.Fakes
{
    /// <summary>
    /// Synchronous in-memory stub for <see cref="IPaymentApiClient"/>.
    /// All methods complete immediately so tests can drive async services
    /// without a real network call or an async test runner.
    /// </summary>
    public sealed class FakePaymentApiClient : IPaymentApiClient
    {
        public PaymentResult<List<PaymentProduct>> ProductsResult { get; set; }
        public PaymentResult<PaymentRequestResponseDto> RequestPaymentResult { get; set; }
        public PaymentResult<List<ClaimItemDto>> ClaimResult { get; set; }

        public int ClaimCallCount { get; private set; }
        public string LastClaimPlayerId { get; private set; }
        public string LastClaimOrderId { get; private set; }

        public Task<PaymentResult<List<PaymentProduct>>> GetProductsAsync()
        {
            return Task.FromResult(ProductsResult);
        }

        public Task<PaymentResult<PaymentRequestResponseDto>> RequestPaymentAsync(
            string playerId,
            string productKey
        )
        {
            return Task.FromResult(RequestPaymentResult);
        }

        public Task<PaymentResult<List<ClaimItemDto>>> ClaimAsync(
            string playerId,
            string orderId = null
        )
        {
            ClaimCallCount++;
            LastClaimPlayerId = playerId;
            LastClaimOrderId = orderId;
            return Task.FromResult(ClaimResult);
        }
    }
}
