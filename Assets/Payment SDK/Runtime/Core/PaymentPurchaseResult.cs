using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PaymentPurchaseResult
    {
        public string OrderId;
        public string ProductKey;
        public string TransactionId;
        public string Receipt;
    }
}