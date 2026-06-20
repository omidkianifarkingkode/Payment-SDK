using GamePaymentSDK.Storage;
using NUnit.Framework;

namespace GamePaymentSDK.Tests.EditMode
{
    public sealed class PlayerPrefsProcessedTransactionStorageTests
    {
        private PlayerPrefsProcessedTransactionStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new PlayerPrefsProcessedTransactionStorage(
                "player-1"
            );
            _storage.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Clear();
        }

        [Test]
        public void MarkProcessed_ThenIsProcessed_ReturnsTrue()
        {
            _storage.MarkProcessed("txn-1", "ZPR_1", "gem_small");

            Assert.IsTrue(_storage.IsProcessed("txn-1"));
            Assert.IsFalse(_storage.IsProcessed("txn-unknown"));
        }

        [Test]
        public void MarkProcessed_SameTransactionTwice_DoesNotDuplicate()
        {
            _storage.MarkProcessed("txn-1", "ZPR_1", "gem_small");
            _storage.MarkProcessed("txn-1", "ZPR_1", "gem_small");

            Assert.AreEqual(1, _storage.GetAll().Count);
        }

        [Test]
        public void MarkProcessed_EmptyTransactionId_IsIgnored()
        {
            _storage.MarkProcessed("", "ZPR_1", "gem_small");

            Assert.AreEqual(0, _storage.GetAll().Count);
        }

        [Test]
        public void Remove_DeletesTransaction()
        {
            _storage.MarkProcessed("txn-1", "ZPR_1", "gem_small");
            _storage.MarkProcessed("txn-2", "ZPR_2", "gem_big");

            _storage.Remove("txn-1");

            Assert.IsFalse(_storage.IsProcessed("txn-1"));
            Assert.IsTrue(_storage.IsProcessed("txn-2"));
        }

        [Test]
        public void RemoveOlderThan_RemovesEntriesBelowThreshold()
        {
            // ProcessedAtUnixSeconds is stamped with UnixTime.NowSeconds() on mark,
            // so a far-future threshold removes everything.
            _storage.MarkProcessed("txn-1", "ZPR_1", "gem_small");

            long farFuture = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 100_000;
            int removed = _storage.RemoveOlderThan(farFuture);

            Assert.AreEqual(1, removed);
            Assert.IsFalse(_storage.IsProcessed("txn-1"));
        }
    }
}
