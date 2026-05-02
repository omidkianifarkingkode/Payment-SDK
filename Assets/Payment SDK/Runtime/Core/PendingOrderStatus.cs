namespace GamePaymentSDK.Core
{
    public static class PendingOrderStatus
    {
        public const string Created = "Created";
        public const string PaymentUrlReceived = "PaymentUrlReceived";
        public const string WebViewOpened = "WebViewOpened";
        public const string CallbackDetected = "CallbackDetected";
        public const string ClaimPending = "ClaimPending";
        public const string Claimed = "Claimed";
        public const string Failed = "Failed";
        public const string ExpiredLocal = "ExpiredLocal";
    }
}