using System.Collections.Generic;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;
using GamePaymentSDK.Services;
using GamePaymentSDK.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace GamePaymentSDK.Tests.EditMode
{
    public sealed class PaymentClaimServiceTests
    {
        private FakePaymentApiClient _api;
        private InMemoryPendingOrderStorage _storage;
        private PaymentClaimService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new FakePaymentApiClient();
            _storage = new InMemoryPendingOrderStorage();
            _service = new PaymentClaimService(_api, _storage, UnityEngine.Debug.unityLogger);
        }

        private static PendingOrder Order(string orderId)
        {
            return new PendingOrder
            {
                OrderId = orderId,
                PlayerId = "player-1",
                ProductKey = "gem_small",
                CreatedAtUnixSeconds = 1000,
                Status = PendingOrderStatus.Created
            };
        }

        [Test]
        public void ClaimOrderAsync_EmptyPlayerId_FailsWithInvalidPlayerId()
        {
            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimOrderAsync("", "ZPR_1").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.InvalidPlayerId, result.FailureReason);
        }

        [Test]
        public void ClaimOrderAsync_EmptyOrderId_FailsWithBadRequest()
        {
            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimOrderAsync("player-1", "").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.BadRequest, result.FailureReason);
        }

        [Test]
        public void ClaimOrderAsync_Success_ReturnsPurchasesAndRemovesPendingOrder()
        {
            _storage.Save(Order("ZPR_1"));
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Ok(new List<ClaimItemDto>
            {
                new ClaimItemDto { orderId = "ZPR_1", productKey = "gem_small" }
            });

            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimOrderAsync("player-1", "ZPR_1").GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual("ZPR_1", result.Data[0].OrderId);
            Assert.AreEqual("gem_small", result.Data[0].ProductKey);
            Assert.IsFalse(string.IsNullOrEmpty(result.Data[0].Receipt));
            Assert.IsFalse(_storage.TryGet("ZPR_1", out _), "claimed order should be removed");
        }

        [Test]
        public void ClaimOrderAsync_ApiFailure_IncrementsClaimAttemptAndPropagatesReason()
        {
            _storage.Save(Order("ZPR_1"));
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Fail(
                PaymentFailureReason.NetworkError, "boom");

            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimOrderAsync("player-1", "ZPR_1").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.NetworkError, result.FailureReason);

            _storage.TryGet("ZPR_1", out PendingOrder order);
            Assert.IsNotNull(order, "order must remain pending for later recovery");
            Assert.AreEqual(1, order.ClaimAttemptCount);
        }

        [Test]
        public void ClaimOrderAsync_SetsStatusToClaimPendingBeforeCallingApi()
        {
            _storage.Save(Order("ZPR_1"));
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Fail(
                PaymentFailureReason.ServerError, "later");

            _service.ClaimOrderAsync("player-1", "ZPR_1").GetAwaiter().GetResult();

            _storage.TryGet("ZPR_1", out PendingOrder order);
            Assert.AreEqual(PendingOrderStatus.ClaimPending, order.Status);
        }

        [Test]
        public void ClaimAllForPlayerAsync_PassesNullOrderIdToApi()
        {
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Ok(new List<ClaimItemDto>());

            _service.ClaimAllForPlayerAsync("player-1").GetAwaiter().GetResult();

            Assert.IsNull(_api.LastClaimOrderId);
            Assert.AreEqual("player-1", _api.LastClaimPlayerId);
        }

        [Test]
        public void ClaimAllForPlayerAsync_Failure_IncrementsAttemptOnAllLocalOrders()
        {
            _storage.Save(Order("ZPR_1"));
            _storage.Save(Order("ZPR_2"));
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Fail(
                PaymentFailureReason.NetworkError, "offline");

            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimAllForPlayerAsync("player-1").GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            foreach (PendingOrder order in _storage.GetAll())
                Assert.AreEqual(1, order.ClaimAttemptCount);
        }

        [Test]
        public void ClaimAllForPlayerAsync_Success_SkipsItemsMissingRequiredFields()
        {
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Ok(new List<ClaimItemDto>
            {
                new ClaimItemDto { orderId = "ZPR_1", productKey = "gem_small" },
                new ClaimItemDto { orderId = "", productKey = "gem_big" },
                null
            });

            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimAllForPlayerAsync("player-1").GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual("ZPR_1", result.Data[0].OrderId);
        }

        [Test]
        public void ClaimLocalPendingOrdersAsync_DelegatesToClaimAll()
        {
            _storage.Save(Order("ZPR_1"));
            _api.ClaimResult = PaymentResult<List<ClaimItemDto>>.Ok(new List<ClaimItemDto>
            {
                new ClaimItemDto { orderId = "ZPR_1", productKey = "gem_small" }
            });

            PaymentResult<List<PaymentPurchaseResult>> result =
                _service.ClaimLocalPendingOrdersAsync("player-1").GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, _api.ClaimCallCount);
            Assert.IsNull(_api.LastClaimOrderId);
        }
    }
}
