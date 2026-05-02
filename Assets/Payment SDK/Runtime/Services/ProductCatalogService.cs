///flow: 
/// 1. App start
/// 2. ProductCatalogService.InitializeAsync()
/// 3. GET /v1/products
/// 4. ProductCatalogCache stores products in RAM
/// 5. Purchase flow only checks RAM cache
/// 
/// Fast product access
/// Single product loading point
/// Clean Unity IAP product mapping later
/// No repeated product API calls during purchase

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public sealed class ProductCatalogService : IProductCatalogService
    {
        private readonly IPaymentApiClient _apiClient;
        private readonly ProductCatalogCache _cache;

        private bool _isInitializing;

        public bool IsReady => _cache.IsReady;

        public ProductCatalogService(
            IPaymentApiClient apiClient,
            ProductCatalogCache cache
        )
        {
            _apiClient = apiClient;
            _cache = cache;
        }

        public async Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> InitializeAsync()
        {
            if (_isInitializing)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.StoreUnavailable,
                    "Product catalog initialization is already running."
                );
            }

            if (IsReady)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Ok(
                    _cache.GetAll()
                );
            }

            return await LoadProductsAsync("Initialize");
        }

        public async Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> RefreshAsync()
        {
            if (_isInitializing)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.StoreUnavailable,
                    "Product catalog refresh is already running."
                );
            }

            return await LoadProductsAsync("Refresh");
        }

        public IReadOnlyCollection<PaymentProduct> GetProducts()
        {
            return _cache.GetAll();
        }

        public bool TryGetProduct(string productKey, out PaymentProduct product)
        {
            product = null;

            if (string.IsNullOrWhiteSpace(productKey))
                return false;

            return _cache.TryGetProduct(productKey, out product);
        }

        private async Task<PaymentResult<IReadOnlyCollection<PaymentProduct>>> LoadProductsAsync(
            string operationName
        )
        {
            if (_apiClient == null)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentApiClient is null."
                );
            }

            if (_cache == null)
            {
                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "ProductCatalogCache is null."
                );
            }

            _isInitializing = true;

            try
            {
                PaymentLogger.Log($"{operationName} product catalog started.");

                PaymentResult<List<PaymentProduct>> result =
                    await _apiClient.GetProductsAsync();

                if (!result.Success)
                {
                    PaymentLogger.LogWarning(
                        $"{operationName} product catalog failed. Reason={result.FailureReason}, Error={result.ErrorMessage}"
                    );

                    return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                        result.FailureReason,
                        result.ErrorMessage
                    );
                }

                List<PaymentProduct> validProducts = FilterValidProducts(result.Data);

                _cache.SetProducts(validProducts);

                PaymentLogger.Log(
                    $"{operationName} product catalog completed. Count={validProducts.Count}"
                );

                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Ok(
                    _cache.GetAll()
                );
            }
            catch (Exception exception)
            {
                PaymentLogger.LogError(
                    $"{operationName} product catalog exception. {exception.Message}"
                );

                return PaymentResult<IReadOnlyCollection<PaymentProduct>>.Fail(
                    PaymentFailureReason.Unknown,
                    exception.Message
                );
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private List<PaymentProduct> FilterValidProducts(List<PaymentProduct> products)
        {
            List<PaymentProduct> validProducts = new List<PaymentProduct>();

            if (products == null)
                return validProducts;

            HashSet<string> seenProductKeys = new HashSet<string>();

            foreach (PaymentProduct product in products)
            {
                if (product == null)
                    continue;

                if (string.IsNullOrWhiteSpace(product.ProductKey))
                    continue;

                if (string.IsNullOrWhiteSpace(product.Name))
                    continue;

                if (product.Price <= 0)
                    continue;

                if (string.IsNullOrWhiteSpace(product.Currency))
                    continue;

                if (!seenProductKeys.Add(product.ProductKey))
                {
                    PaymentLogger.LogWarning(
                        $"Duplicate productKey ignored. productKey={product.ProductKey}"
                    );

                    continue;
                }

                validProducts.Add(product);
            }

            return validProducts;
        }
    }
}