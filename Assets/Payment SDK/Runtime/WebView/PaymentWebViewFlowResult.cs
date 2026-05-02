using GamePaymentSDK.Core;

namespace GamePaymentSDK.WebView
{
    public sealed class PaymentWebViewFlowResult
    {
        public PaymentWebViewCompletionType CompletionType;
        public PaymentCallbackResult CallbackResult;
        public string ErrorMessage;

        public bool HasCallback =>
            CompletionType == PaymentWebViewCompletionType.CallbackDetected &&
            CallbackResult != null &&
            CallbackResult.IsPaymentCallback;
    }
}