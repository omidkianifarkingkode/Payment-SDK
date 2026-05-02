using System;

namespace GamePaymentSDK.Api
{
    [Serializable]
    public sealed class ProductsResponseDto
    {
        public ProductDto[] products;
    }

    [Serializable]
    public sealed class ProductDto
    {
        public string productKey;
        public string name;
        public double price;
        public string currency;
    }

    [Serializable]
    public sealed class PaymentRequestDto
    {
        public string playerId;
        public string productKey;
    }

    [Serializable]
    public sealed class PaymentRequestResponseDto
    {
        public string orderId;
        public string paymentUrl;
    }

    [Serializable]
    public sealed class ClaimRequestDto
    {
        public string playerId;
        public string orderId;
    }

    [Serializable]
    public sealed class ClaimItemDto
    {
        public string orderId;
        public string productKey;
    }

    [Serializable]
    public sealed class ClaimItemsWrapperDto
    {
        public ClaimItemDto[] items;
    }
}