using SdkFailureReason = GamePaymentSDK.Core.PaymentFailureReason;
using UnityPurchaseFailureReason = UnityEngine.Purchasing.PurchaseFailureReason;

namespace GamePaymentSDK.UnityIAP
{
    public static class GamePaymentIapFailureMapper
    {
        public static UnityPurchaseFailureReason ToUnityFailureReason(
            SdkFailureReason reason
        )
        {
            return reason switch
            {
                SdkFailureReason.ProductUnavailable => UnityPurchaseFailureReason.ProductUnavailable,
                SdkFailureReason.ProductNotFound => UnityPurchaseFailureReason.ProductUnavailable,
                SdkFailureReason.NotInitialized => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.StoreUnavailable => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.InvalidConfiguration => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.InvalidApiKey => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.Unauthorized => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.Forbidden => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.NetworkError => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.ServerError => UnityPurchaseFailureReason.PurchasingUnavailable,
                SdkFailureReason.WebViewClosedByUser => UnityPurchaseFailureReason.UserCancelled,
                SdkFailureReason.PaymentCancelled => UnityPurchaseFailureReason.UserCancelled,
                SdkFailureReason.PurchaseAlreadyInProgress => UnityPurchaseFailureReason.ExistingPurchasePending,
                SdkFailureReason.AlreadyClaimed => UnityPurchaseFailureReason.DuplicateTransaction,
                _ => UnityPurchaseFailureReason.Unknown
            };
        }
    }
}