namespace GamePaymentSDK.Core
{
    public sealed class PaymentResult
    {
        public bool Success { get; }
        public PaymentFailureReason FailureReason { get; }
        public string ErrorMessage { get; }

        private PaymentResult(bool success, PaymentFailureReason failureReason, string errorMessage)
        {
            Success = success;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
        }

        public static PaymentResult Ok()
        {
            return new PaymentResult(true, PaymentFailureReason.None, null);
        }

        public static PaymentResult Fail(PaymentFailureReason reason, string errorMessage = null)
        {
            return new PaymentResult(false, reason, errorMessage);
        }
    }

    public sealed class PaymentResult<T>
    {
        public bool Success { get; }
        public T Data { get; }
        public PaymentFailureReason FailureReason { get; }
        public string ErrorMessage { get; }

        private PaymentResult(bool success, T data, PaymentFailureReason failureReason, string errorMessage)
        {
            Success = success;
            Data = data;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
        }

        public static PaymentResult<T> Ok(T data)
        {
            return new PaymentResult<T>(true, data, PaymentFailureReason.None, null);
        }

        public static PaymentResult<T> Fail(PaymentFailureReason reason, string errorMessage = null)
        {
            return new PaymentResult<T>(false, default, reason, errorMessage);
        }
    }
}