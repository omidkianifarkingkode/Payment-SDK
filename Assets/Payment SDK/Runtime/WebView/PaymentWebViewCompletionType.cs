namespace GamePaymentSDK.WebView
{
    public enum PaymentWebViewCompletionType
    {
        Unknown = 0,
        CallbackDetected = 1,
        ClosedByUser = 2,
        LoadFailed = 3,
        Timeout = 4,
        OpenFailed = 5
    }
}