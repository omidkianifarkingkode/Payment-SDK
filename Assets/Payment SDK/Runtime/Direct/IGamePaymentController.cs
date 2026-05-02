using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Direct
{
    public interface IGamePaymentController
    {
        event Action<PaymentInitializedEventArgs> Initialized;
        event Action<IReadOnlyCollection<PaymentProduct>> ProductsUpdated;
        event Action<PaymentPurchaseResult> PurchaseSucceeded;
        event Action<PaymentPurchaseFailedEventArgs> PurchaseFailed;

        bool IsInitialized { get; }
        bool IsInitializing { get; }
        bool IsPurchaseInProgress { get; }

        string PlayerId { get; }

        Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> InitializeAsync();

        IReadOnlyCollection<PaymentProduct> GetProducts();

        bool TryGetProduct(string productKey, out PaymentProduct product);

        Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(string productKey);

        Task<PaymentResult<List<PaymentPurchaseResult>>> ClaimPendingPurchasesAsync();

        void ConfirmPurchaseProcessed(PaymentPurchaseResult purchase);

        void Dispose();
    }
}