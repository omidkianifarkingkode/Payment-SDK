using System.Collections.Generic;
using System.Linq;
using GamePaymentSDK.Core;
using GamePaymentSDK.Storage;

namespace GamePaymentSDK.Tests.EditMode.Fakes
{
    /// <summary>
    /// In-memory <see cref="IPendingOrderStorage"/> used to keep service tests
    /// free of PlayerPrefs side effects. The PlayerPrefs implementation itself
    /// is covered by dedicated storage tests.
    /// </summary>
    public sealed class InMemoryPendingOrderStorage : IPendingOrderStorage
    {
        private readonly Dictionary<string, PendingOrder> _orders = new();

        public void Save(PendingOrder order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.OrderId))
                return;

            _orders[order.OrderId] = order;
        }

        public bool TryGet(string orderId, out PendingOrder order)
        {
            return _orders.TryGetValue(orderId ?? string.Empty, out order);
        }

        public List<PendingOrder> GetAll()
        {
            return _orders.Values.ToList();
        }

        public void Remove(string orderId)
        {
            if (!string.IsNullOrWhiteSpace(orderId))
                _orders.Remove(orderId);
        }

        public int RemoveOlderThan(long unixSeconds)
        {
            List<string> toRemove = _orders.Values
                .Where(o => o.CreatedAtUnixSeconds < unixSeconds)
                .Select(o => o.OrderId)
                .ToList();

            foreach (string id in toRemove)
                _orders.Remove(id);

            return toRemove.Count;
        }

        public void Clear()
        {
            _orders.Clear();
        }
    }
}
