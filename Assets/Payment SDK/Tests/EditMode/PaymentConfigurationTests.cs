using GamePaymentSDK.Core;
using NUnit.Framework;

namespace GamePaymentSDK.Tests.EditMode
{
    public sealed class PaymentConfigurationTests
    {
        private static PaymentConfiguration ValidConfig()
        {
            return new PaymentConfiguration
            {
                BaseUrl = "https://api.example.com/api",
                ApiKey = "test-api-key",
            };
        }

        [Test]
        public void IsValid_WithAllRequiredFields_ReturnsTrue()
        {
            PaymentConfiguration config = ValidConfig();

            bool valid = config.IsValid(out string error);

            Assert.IsTrue(valid);
            Assert.IsNull(error);
        }

        [Test]
        public void IsValid_MissingBaseUrl_FailsWithBaseUrlError()
        {
            PaymentConfiguration config = ValidConfig();
            config.BaseUrl = "   ";

            bool valid = config.IsValid(out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("BaseUrl is required.", error);
        }

        [Test]
        public void IsValid_MissingApiKey_FailsWithApiKeyError()
        {
            PaymentConfiguration config = ValidConfig();
            config.ApiKey = null;

            bool valid = config.IsValid(out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("ApiKey is required.", error);
        }

        [Test]
        public void IsValid_MissingClientId_FailsWithClientIdError()
        {
            PaymentConfiguration config = ValidConfig();

            bool valid = config.IsValid(out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("ClientId is required.", error);
        }

        [Test]
        public void GetNormalizedBaseUrl_TrimsTrailingSlashes()
        {
            PaymentConfiguration config = ValidConfig();
            config.BaseUrl = "https://api.example.com/api/";

            Assert.AreEqual("https://api.example.com/api", config.GetNormalizedBaseUrl());
        }

        [Test]
        public void GetNormalizedBaseUrl_WithEmptyBaseUrl_ReturnsEmpty()
        {
            PaymentConfiguration config = ValidConfig();
            config.BaseUrl = null;

            Assert.AreEqual(string.Empty, config.GetNormalizedBaseUrl());
        }
    }
}
