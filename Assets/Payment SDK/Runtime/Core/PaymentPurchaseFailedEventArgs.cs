using System;

namespace GamePaymentSDK.Core
{
    public sealed class PaymentPurchaseFailedEventArgs : EventArgs
    {
        public string ProductKey { get; }
        public PaymentFailureReason FailureReason { get; }
        public string ErrorMessage { get; }

        public PaymentPurchaseFailedEventArgs(
            string productKey,
            PaymentFailureReason failureReason,
            string errorMessage
        )
        {
            ProductKey = productKey;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
        }
    }
}