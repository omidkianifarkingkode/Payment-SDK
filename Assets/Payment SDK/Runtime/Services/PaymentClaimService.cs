using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;
using GamePaymentSDK.Storage;

namespace GamePaymentSDK.Services
{
    public sealed class PaymentClaimService : IPaymentClaimService
    {
        private readonly IPaymentApiClient _apiClient;
        private readonly IPendingOrderStorage _pendingOrderStorage;

        public PaymentClaimService(
            IPaymentApiClient apiClient,
            IPendingOrderStorage pendingOrderStorage
        )
        {
            _apiClient = apiClient;
            _pendingOrderStorage = pendingOrderStorage;
        }

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimOrderAsync(
            string playerId,
            string orderId
        )
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.BadRequest,
                    "orderId is required."
                );
            }

            MarkOrderAsClaimPending(orderId);

            PaymentLogger.Log($"Claim specific order started. orderId={orderId}");

            PaymentResult<List<ClaimItemDto>> claimResult =
                await _apiClient.ClaimAsync(playerId, orderId);

            if (!claimResult.Success)
            {
                RegisterClaimFailure(orderId);

                PaymentLogger.LogWarning(
                    $"Claim specific order failed. orderId={orderId}, reason={claimResult.FailureReason}, error={claimResult.ErrorMessage}"
                );

                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    claimResult.FailureReason,
                    claimResult.ErrorMessage
                );
            }

            List<PaymentPurchaseResult> purchases =
                ConvertClaimItemsToPurchaseResults(playerId, claimResult.Data);

            RemoveClaimedOrdersFromStorage(purchases);

            PaymentLogger.Log(
                $"Claim specific order completed. orderId={orderId}, claimedCount={purchases.Count}"
            );

            return PaymentResult<List<PaymentPurchaseResult>>.Ok(purchases);
        }

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimAllForPlayerAsync(
            string playerId
        )
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            PaymentLogger.Log($"Claim all for player started. playerId={playerId}");

            PaymentResult<List<ClaimItemDto>> claimResult =
                await _apiClient.ClaimAsync(playerId, null);

            if (!claimResult.Success)
            {
                RegisterAllLocalClaimFailures();

                PaymentLogger.LogWarning(
                    $"Claim all for player failed. playerId={playerId}, reason={claimResult.FailureReason}, error={claimResult.ErrorMessage}"
                );

                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    claimResult.FailureReason,
                    claimResult.ErrorMessage
                );
            }

            List<PaymentPurchaseResult> purchases =
                ConvertClaimItemsToPurchaseResults(playerId, claimResult.Data);

            RemoveClaimedOrdersFromStorage(purchases);

            PaymentLogger.Log(
                $"Claim all for player completed. playerId={playerId}, claimedCount={purchases.Count}"
            );

            return PaymentResult<List<PaymentPurchaseResult>>.Ok(purchases);
        }

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimLocalPendingOrdersAsync(
            string playerId
        )
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            List<PendingOrder> localPendingOrders = _pendingOrderStorage.GetAll();

            PaymentLogger.Log(
                $"Claim local pending orders started. localPendingCount={localPendingOrders.Count}"
            );

            /*
             * Important:
             * We do not need to call claim once for every local order.
             *
             * Backend should support:
             * POST /v1/payments/claim
             * {
             *     "playerId": "...",
             *     "orderId": null
             * }
             *
             * Meaning:
             * claim all unclaimed verified orders for this player.
             */
            PaymentResult<List<PaymentPurchaseResult>> result =
                await ClaimAllForPlayerAsync(playerId);

            if (!result.Success)
                return result;

            PaymentLogger.Log(
                $"Claim local pending orders completed. claimedCount={result.Data.Count}"
            );

            return result;
        }

        private List<PaymentPurchaseResult> ConvertClaimItemsToPurchaseResults(
            string playerId,
            List<ClaimItemDto> claimItems
        )
        {
            List<PaymentPurchaseResult> purchases = new();

            if (claimItems == null)
                return purchases;

            foreach (ClaimItemDto item in claimItems)
            {
                if (item == null)
                    continue;

                if (string.IsNullOrWhiteSpace(item.orderId))
                    continue;

                if (string.IsNullOrWhiteSpace(item.productKey))
                    continue;

                string receipt = PaymentReceiptBuilder.Build(
                    item.orderId,
                    item.productKey,
                    playerId
                );

                PaymentPurchaseResult purchase = new PaymentPurchaseResult
                {
                    OrderId = item.orderId,
                    ProductKey = item.productKey,
                    TransactionId = item.orderId,
                    Receipt = receipt
                };

                purchases.Add(purchase);
            }

            return purchases;
        }

        private void RemoveClaimedOrdersFromStorage(
            List<PaymentPurchaseResult> purchases
        )
        {
            if (purchases == null)
                return;

            foreach (PaymentPurchaseResult purchase in purchases)
            {
                if (purchase == null)
                    continue;

                if (string.IsNullOrWhiteSpace(purchase.OrderId))
                    continue;

                _pendingOrderStorage.Remove(purchase.OrderId);
            }
        }

        private void MarkOrderAsClaimPending(string orderId)
        {
            if (!_pendingOrderStorage.TryGet(orderId, out PendingOrder order))
                return;

            order.Status = PendingOrderStatus.ClaimPending;
            _pendingOrderStorage.Save(order);
        }

        private void RegisterClaimFailure(string orderId)
        {
            if (!_pendingOrderStorage.TryGet(orderId, out PendingOrder order))
                return;

            order.ClaimAttemptCount++;
            _pendingOrderStorage.Save(order);
        }

        private void RegisterAllLocalClaimFailures()
        {
            List<PendingOrder> orders = _pendingOrderStorage.GetAll();

            foreach (PendingOrder order in orders)
            {
                if (order == null)
                    continue;

                order.ClaimAttemptCount++;
                _pendingOrderStorage.Save(order);
            }
        }
    }
}