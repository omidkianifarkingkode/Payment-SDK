using GamePaymentSDK.Services;
using NUnit.Framework;

namespace GamePaymentSDK.Tests.EditMode
{
    public sealed class PaymentCallbackParserTests
    {
        private const string ClientId = "client-123";

        private static PaymentCallbackParser CreateParser()
        {
            return new PaymentCallbackParser(UnityEngine.Debug.unityLogger);
        }

        [Test]
        public void Parse_ValidCallbackWithStatusOk_IsSuccess()
        {
            PaymentCallbackParser parser = CreateParser();

            string url = $"https://api.example.com/v1/payments/callback/{ClientId}?authority=A0000001&status=OK";
            PaymentCallbackResult result = parser.Parse(url);

            Assert.IsTrue(result.IsPaymentCallback);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PaymentCallbackStatus.Ok, result.Status);
            Assert.AreEqual("A0000001", result.Authority);
            Assert.AreEqual(ClientId, result.ClientId);
        }

        [Test]
        public void Parse_StatusNok_IsCallbackButNotSuccess()
        {
            PaymentCallbackParser parser = CreateParser();

            string url = $"https://api.example.com/v1/payments/callback/{ClientId}?authority=A1&status=NOK";
            PaymentCallbackResult result = parser.Parse(url);

            Assert.IsTrue(result.IsPaymentCallback);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaymentCallbackStatus.Nok, result.Status);
        }

        [Test]
        public void Parse_StatusIsCaseInsensitive()
        {
            PaymentCallbackParser parser = CreateParser();

            string url = $"https://api.example.com/v1/payments/callback/{ClientId}?status=ok";
            PaymentCallbackResult result = parser.Parse(url);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PaymentCallbackStatus.Ok, result.Status);
        }

        [Test]
        public void Parse_UnrelatedUrl_IsNotPaymentCallback()
        {
            PaymentCallbackParser parser = CreateParser();

            PaymentCallbackResult result = parser.Parse("https://api.example.com/v1/other?status=OK");

            Assert.IsFalse(result.IsPaymentCallback);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_CallbackForDifferentClientId_IsNotPaymentCallback()
        {
            PaymentCallbackParser parser = CreateParser();

            string url = "https://api.example.com/v1/payments/callback/some-other-client?status=OK";
            PaymentCallbackResult result = parser.Parse(url);

            Assert.IsFalse(result.IsPaymentCallback);
        }

        [Test]
        public void Parse_NullOrEmptyUrl_IsNotPaymentCallback()
        {
            PaymentCallbackParser parser = CreateParser();

            Assert.IsFalse(parser.Parse(null).IsPaymentCallback);
            Assert.IsFalse(parser.Parse("").IsPaymentCallback);
        }

        [Test]
        public void Parse_MalformedUrl_DoesNotThrowAndIsNotCallback()
        {
            PaymentCallbackParser parser = CreateParser();

            PaymentCallbackResult result = parser.Parse("not a url");

            Assert.IsFalse(result.IsPaymentCallback);
        }

        [Test]
        public void Parse_MissingStatus_IsCallbackWithUnknownStatus()
        {
            PaymentCallbackParser parser = CreateParser();

            string url = $"https://api.example.com/v1/payments/callback/{ClientId}?authority=A1";
            PaymentCallbackResult result = parser.Parse(url);

            Assert.IsTrue(result.IsPaymentCallback);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaymentCallbackStatus.Unknown, result.Status);
        }
    }
}
