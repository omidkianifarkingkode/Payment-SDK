using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PaymentStartResult
    {
        public string OrderId;
        public string PlayerId;
        public string ProductKey;
        public string PaymentUrl;
    }
}