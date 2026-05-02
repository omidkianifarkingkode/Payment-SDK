/// persists unfinished orders locally so the SDK can recover purchases after:
/// * App crash
/// * Internet disconnect
/// * WebView closed
/// * Claim failed
/// * Player restarted the game

using System.Collections.Generic;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Storage
{
    public interface IPendingOrderStorage
    {
        void Save(PendingOrder order);

        bool TryGet(string orderId, out PendingOrder order);

        List<PendingOrder> GetAll();

        void Remove(string orderId);

        int RemoveOlderThan(long unixSeconds);

        void Clear();
    }
}