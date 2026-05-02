using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PaymentCallbackResult
    {
        public bool IsPaymentCallback;
        public bool IsSuccess;

        public string RawUrl;
        public string ClientId;
        public string Authority;
        public string StatusRaw;

        public PaymentCallbackStatus Status;
    }
}