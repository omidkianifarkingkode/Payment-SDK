using System;

namespace GamePaymentSDK.Core
{
    [Serializable]
    public sealed class PaymentProduct
    {
        public string ProductKey { get; }
        public string Name { get; }
        public double Price { get; }
        public string Currency { get; }

        public PaymentProduct(string productKey, string name, double price, string currency)
        {
            ProductKey = productKey;
            Name = name;
            Price = price;
            Currency = currency;
        }

        public string GetLocalizedPriceString()
        {
            return $"{Price:0} {Currency}";
        }
    }
}