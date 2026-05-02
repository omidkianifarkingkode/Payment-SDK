using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PaymentConfiguration
    {
        public string BaseUrl;
        public string ApiKey;
        public string ClientId;
        public PaymentEnvironment Environment = PaymentEnvironment.Production;

        public int RequestTimeoutSeconds = 20;
        public int WebViewTimeoutSeconds = 300;
        public int ClaimRetryCount = 3;
        public int PendingOrderCleanupDays = 7;
        public int ProcessedTransactionHistoryDays = 90;
        public bool EnableLogs = true;

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                error = "BaseUrl is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                error = "ApiKey is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ClientId))
            {
                error = "ClientId is required.";
                return false;
            }

            error = null;
            return true;
        }

        public string GetNormalizedBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                return string.Empty;

            return BaseUrl.TrimEnd('/');
        }
    }
}