using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PendingOrder
    {
        public string OrderId;
        public string PlayerId;
        public string ProductKey;
        public string PaymentUrl;
        public long CreatedAtUnixSeconds;
        public int ClaimAttemptCount;
        public string Status;
    }
}