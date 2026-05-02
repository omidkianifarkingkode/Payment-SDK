namespace GamePaymentSDK.Core
{
    public enum PaymentFailureReason
    {
        None = 0,

        NotInitialized = 10,
        StoreUnavailable = 11,
        ProductUnavailable = 12,
        ProductNotFound = 13,

        InvalidConfiguration = 20,
        InvalidPlayerId = 21,
        InvalidApiKey = 22,

        NetworkError = 30,
        ServerError = 31,
        Unauthorized = 32,
        Forbidden = 33,
        BadRequest = 34,

        PaymentRequestFailed = 40,
        WebViewOpenFailed = 41,
        WebViewClosedByUser = 42,
        PaymentCancelled = 43,
        PaymentCallbackFailed = 44,
        PurchaseAlreadyInProgress = 45,
        WebViewTimeout = 46,

        ClaimFailed = 50,
        AlreadyClaimed = 51,
        PaymentNotVerified = 52,

        Unknown = 999
    }
}