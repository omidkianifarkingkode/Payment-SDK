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

            // Fields are private [SerializeField]; set them via reflection for the test
            // rather than widening the runtime API.
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
        public void ToConfiguration_MapsBaseUrlAndApiKey()
        {
            PaymentSettings settings = CreateSettings(
                "https://api.example.com/api", "test-api-key", "player-1");

            PaymentConfiguration config = settings.ToConfiguration();

            Assert.AreEqual("https://api.example.com/api", config.BaseUrl);
            Assert.AreEqual("test-api-key", config.ApiKey);
            // PlayerId is not part of PaymentConfiguration; it is exposed on the asset.
            Assert.AreEqual("player-1", settings.PlayerId);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ToConfiguration_ProducesValidConfig_WhenRequiredFieldsPresent()
        {
            PaymentSettings settings = CreateSettings(
                "https://api.example.com/api", "test-api-key", "player-1");

            bool valid = settings.ToConfiguration().IsValid(out string error);

            Assert.IsTrue(valid);
            Assert.IsNull(error);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ToConfiguration_ProducesInvalidConfig_WhenEmpty()
        {
            PaymentSettings settings = CreateSettings(string.Empty, string.Empty, string.Empty);

            bool valid = settings.ToConfiguration().IsValid(out string error);

            Assert.IsFalse(valid);
            Assert.IsNotNull(error);

            Object.DestroyImmediate(settings);
        }
    }
}
