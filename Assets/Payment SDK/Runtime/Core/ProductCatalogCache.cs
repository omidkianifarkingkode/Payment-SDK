using System.Collections.Generic;

namespace GamePaymentSDK.Core
{
    public sealed class ProductCatalogCache
    {
        private readonly Dictionary<string, PaymentProduct> _products = new();

        public bool IsReady { get; private set; }

        public void SetProducts(IEnumerable<PaymentProduct> products)
        {
            _products.Clear();

            foreach (PaymentProduct product in products)
            {
                if (product == null || string.IsNullOrWhiteSpace(product.ProductKey))
                    continue;

                _products[product.ProductKey] = product;
            }

            IsReady = true;
        }

        public bool TryGetProduct(string productKey, out PaymentProduct product)
        {
            return _products.TryGetValue(productKey, out product);
        }

        public IReadOnlyCollection<PaymentProduct> GetAll()
        {
            return _products.Values;
        }

        public void Clear()
        {
            _products.Clear();
            IsReady = false;
        }
    }
}