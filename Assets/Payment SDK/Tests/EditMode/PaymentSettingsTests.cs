using System.Reflection;
using GamePaymentSDK.Core;
using NUnit.Framework;
using UnityEngine;

namespace GamePaymentSDK.Tests.EditMode
{
    public sealed class PaymentSettingsTests
    {
        private static PaymentSettings CreateSettings(string baseUrl, string apiKey, string playerId)
        {
            PaymentSettings settings = ScriptableObject.CreateInstance<PaymentSettings>();
            SetField(settings, "_baseUrl", baseUrl);
            SetField(settings, "_apiKey", apiKey);
            SetField(settings, "_playerId", playerId);
            return settings;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on PaymentSettings.");
            field.SetValue(target, value);
        }

        [Test]
        public void IsValid_WithAllRequiredFields_ReturnsTrue()
        {
            PaymentSettings settings = CreateSettings("https://api.example.com/api", "test-key", "player-1");

            bool valid = settings.IsValid(out string error);

            Assert.IsTrue(valid);
            Assert.IsNull(error);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void IsValid_MissingBaseUrl_FailsWithBaseUrlError()
        {
            PaymentSettings settings = CreateSettings("   ", "test-key", "player-1");

            bool valid = settings.IsValid(out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("BaseUrl is required.", error);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void IsValid_MissingApiKey_FailsWithApiKeyError()
        {
            PaymentSettings settings = CreateSettings("https://api.example.com/api", null, "player-1");

            bool valid = settings.IsValid(out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("ApiKey is required.", error);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GetNormalizedBaseUrl_TrimsTrailingSlash()
        {
            PaymentSettings settings = CreateSettings("https://api.example.com/api/", "key", "player-1");

            Assert.AreEqual("https://api.example.com/api", settings.GetNormalizedBaseUrl());

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GetNormalizedBaseUrl_WithEmptyBaseUrl_ReturnsEmpty()
        {
            PaymentSettings settings = CreateSettings(null, "key", "player-1");

            Assert.AreEqual(string.Empty, settings.GetNormalizedBaseUrl());

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void PlayerId_ReturnsExpectedValue()
        {
            PaymentSettings settings = CreateSettings("https://api.example.com/api", "test-key", "player-1");

            Assert.AreEqual("player-1", settings.PlayerId);

            Object.DestroyImmediate(settings);
        }
    }
}
