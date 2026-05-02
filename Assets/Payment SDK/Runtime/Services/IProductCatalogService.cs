using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public interface IProductCatalogService
    {
        bool IsReady { get; }

        Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> InitializeAsync();

        Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> RefreshAsync();

        IReadOnlyCollection<PaymentProduct> GetProducts();

        bool TryGetProduct(string productKey, out PaymentProduct product);
    }
}