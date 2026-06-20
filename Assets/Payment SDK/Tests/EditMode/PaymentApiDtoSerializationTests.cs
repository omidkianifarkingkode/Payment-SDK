using GamePaymentSDK.Api;
using NUnit.Framework;
using UnityEngine;

namespace GamePaymentSDK.Tests.EditMode
{
    /// <summary>
    /// Locks in the JsonUtility serialization contract the API client relies on,
    /// including the array-wrapping trick used to parse a top-level JSON array
    /// claim response.
    /// </summary>
    public sealed class PaymentApiDtoSerializationTests
    {
        [Test]
        public void PaymentRequestDto_RoundTrips()
        {
            PaymentRequestDto dto = new PaymentRequestDto
            {
                playerId = "player-1",
                productKey = "gem_pack_small"
            };

            string json = JsonUtility.ToJson(dto);
            PaymentRequestDto parsed = JsonUtility.FromJson<PaymentRequestDto>(json);

            Assert.AreEqual("player-1", parsed.playerId);
            Assert.AreEqual("gem_pack_small", parsed.productKey);
        }

        [Test]
        public void PaymentRequestResponseDto_DeserializesServerShape()
        {
            const string serverJson = "{\"orderId\":\"ZPR_123\",\"paymentUrl\":\"https://pay.example.com/x\"}";

            PaymentRequestResponseDto dto = JsonUtility.FromJson<PaymentRequestResponseDto>(serverJson);

            Assert.AreEqual("ZPR_123", dto.orderId);
            Assert.AreEqual("https://pay.example.com/x", dto.paymentUrl);
        }

        [Test]
        public void ProductsResponseDto_DeserializesProductArray()
        {
            const string serverJson =
                "{\"products\":[" +
                "{\"productKey\":\"gem_small\",\"name\":\"Small\",\"price\":50000,\"currency\":\"IRR\"}," +
                "{\"productKey\":\"gem_big\",\"name\":\"Big\",\"price\":150000,\"currency\":\"IRR\"}" +
                "]}";

            ProductsResponseDto dto = JsonUtility.FromJson<ProductsResponseDto>(serverJson);

            Assert.IsNotNull(dto.products);
            Assert.AreEqual(2, dto.products.Length);
            Assert.AreEqual("gem_small", dto.products[0].productKey);
            Assert.AreEqual(50000, dto.products[0].price, 0.001);
            Assert.AreEqual("IRR", dto.products[1].currency);
        }

        [Test]
        public void ClaimItemsWrapper_ParsesWrappedTopLevelArray()
        {
            // Mirrors PaymentApiClient.ParseClaimItems: a top-level JSON array
            // is wrapped as {"items":[...]} before being handed to JsonUtility.
            const string serverArray =
                "[{\"orderId\":\"ZPR_1\",\"productKey\":\"gem_small\"}," +
                "{\"orderId\":\"ZPR_2\",\"productKey\":\"gem_big\"}]";

            string wrapped = "{\"items\":" + serverArray + "}";
            ClaimItemsWrapperDto dto = JsonUtility.FromJson<ClaimItemsWrapperDto>(wrapped);

            Assert.IsNotNull(dto.items);
            Assert.AreEqual(2, dto.items.Length);
            Assert.AreEqual("ZPR_1", dto.items[0].orderId);
            Assert.AreEqual("gem_big", dto.items[1].productKey);
        }

        [Test]
        public void ClaimItemsWrapper_EmptyArray_YieldsEmptyItems()
        {
            string wrapped = "{\"items\":" + "[]" + "}";
            ClaimItemsWrapperDto dto = JsonUtility.FromJson<ClaimItemsWrapperDto>(wrapped);

            Assert.IsNotNull(dto.items);
            Assert.AreEqual(0, dto.items.Length);
        }
    }
}
