using UnityEngine;

namespace GamePaymentSDK.Core
{
    public static class PaymentReceiptBuilder
    {
        private const string StoreName = "GamePaymentSDK";
        private const string ProviderName = "Zarinpal";

        public static string Build(
            string orderId,
            string productKey,
            string playerId
        )
        {
            PaymentReceiptData receipt = new PaymentReceiptData
            {
                store = StoreName,
                provider = ProviderName,
                orderId = orderId,
                transactionId = orderId,
                productKey = productKey,
                playerId = playerId,
                claimedAtUnixSeconds = UnixTime.NowSeconds()
            };

            return JsonUtility.ToJson(receipt);
        }
    }
}