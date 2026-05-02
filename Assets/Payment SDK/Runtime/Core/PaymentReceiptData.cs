using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PaymentReceiptData
    {
        public string store;
        public string provider;
        public string orderId;
        public string transactionId;
        public string productKey;
        public string playerId;
        public long claimedAtUnixSeconds;
    }
}