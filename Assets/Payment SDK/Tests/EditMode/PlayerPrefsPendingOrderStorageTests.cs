using System.Collections.Generic;
using GamePaymentSDK.Core;
using GamePaymentSDK.Storage;
using NUnit.Framework;

namespace GamePaymentSDK.Tests.EditMode
{
    /// <summary>
    /// Exercises the real PlayerPrefs-backed storage. Each test uses a unique
    /// client/player scope and clears it in teardown to avoid leaking state
    /// into editor PlayerPrefs.
    /// </summary>
    public sealed class PlayerPrefsPendingOrderStorageTests
    {
        private PlayerPrefsPendingOrderStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new PlayerPrefsPendingOrderStorage(
                "player-1"
            );
            _storage.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Clear();
        }

        private static PendingOrder Order(string orderId, long createdAt = 1000)
        {
            return new PendingOrder
            {
                OrderId = orderId,
                PlayerId = "player-1",
                ProductKey = "gem_small",
                PaymentUrl = "https://pay.example.com/x",
                CreatedAtUnixSeconds = createdAt,
                Status = PendingOrderStatus.Created
            };
        }

        [Test]
        public void Save_ThenTryGet_ReturnsSameOrder()
        {
            _storage.Save(Order("ZPR_1"));

            bool found = _storage.TryGet("ZPR_1", out PendingOrder order);

            Assert.IsTrue(found);
            Assert.AreEqual("ZPR_1", order.OrderId);
            Assert.AreEqual("gem_small", order.ProductKey);
        }

        [Test]
        public void Save_ExistingOrderId_UpdatesInPlace()
        {
            _storage.Save(Order("ZPR_1"));

            PendingOrder updated = Order("ZPR_1");
            updated.Status = PendingOrderStatus.ClaimPending;
            _storage.Save(updated);

            _storage.TryGet("ZPR_1", out PendingOrder order);

            Assert.AreEqual(1, _storage.GetAll().Count);
            Assert.AreEqual(PendingOrderStatus.ClaimPending, order.Status);
        }

        [Test]
        public void Save_NullOrEmptyOrderId_IsIgnored()
        {
            _storage.Save(null);
            _storage.Save(Order(""));

            Assert.AreEqual(0, _storage.GetAll().Count);
        }

        [Test]
        public void Remove_DeletesOrder()
        {
            _storage.Save(Order("ZPR_1"));
            _storage.Save(Order("ZPR_2"));

            _storage.Remove("ZPR_1");

            Assert.IsFalse(_storage.TryGet("ZPR_1", out _));
            Assert.IsTrue(_storage.TryGet("ZPR_2", out _));
        }

        [Test]
        public void RemoveOlderThan_RemovesOnlyOldOrders()
        {
            _storage.Save(Order("old", createdAt: 100));
            _storage.Save(Order("new", createdAt: 5000));

            int removed = _storage.RemoveOlderThan(1000);

            Assert.AreEqual(1, removed);
            Assert.IsFalse(_storage.TryGet("old", out _));
            Assert.IsTrue(_storage.TryGet("new", out _));
        }

        [Test]
        public void Persistence_NewInstanceWithSameScope_SeesSavedData()
        {
            PlayerPrefsPendingOrderStorage a = new("player-1");
            a.Clear();
            a.Save(Order("ZPR_persist"));

            PlayerPrefsPendingOrderStorage b = new("player-1");
            bool found = b.TryGet("ZPR_persist", out _);

            b.Clear();

            Assert.IsTrue(found);
        }

        [Test]
        public void Scope_DifferentPlayer_DoesNotSeeOtherPlayersOrders()
        {
            PlayerPrefsPendingOrderStorage playerA = new("player-A");
            PlayerPrefsPendingOrderStorage playerB = new("player-B");
            playerA.Clear();
            playerB.Clear();

            playerA.Save(Order("ZPR_A"));

            List<PendingOrder> bOrders = playerB.GetAll();

            playerA.Clear();
            playerB.Clear();

            Assert.AreEqual(0, bOrders.Count);
        }
    }
}
