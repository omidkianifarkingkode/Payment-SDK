using System;
using System.Collections.Generic;

namespace GamePaymentSDK.Core
{
    public sealed class PaymentInitializedEventArgs : EventArgs
    {
        public bool Success { get; }
        public IReadOnlyCollection<PaymentProduct> Products { get; }
        public PaymentFailureReason FailureReason { get; }
        public string ErrorMessage { get; }

        public PaymentInitializedEventArgs(
            bool success,
            IReadOnlyCollection<PaymentProduct> products,
            PaymentFailureReason failureReason,
            string errorMessage
        )
        {
            Success = success;
            Products = products;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
        }
    }
}