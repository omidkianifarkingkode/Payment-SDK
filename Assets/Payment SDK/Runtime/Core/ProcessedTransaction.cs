using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class ProcessedTransaction
    {
        public string TransactionId;
        public string OrderId;
        public string ProductKey;
        public long ProcessedAtUnixSeconds;
    }
}