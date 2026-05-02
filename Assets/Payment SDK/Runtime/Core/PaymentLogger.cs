using UnityEngine;

namespace GamePaymentSDK.Core
{
    public static class PaymentLogger
    {
        private static bool _enabled = true;

        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        public static void Log(string message)
        {
            if (!_enabled)
                return;

            Debug.Log($"[GamePaymentSDK] {message}");
        }

        public static void LogWarning(string message)
        {
            if (!_enabled)
                return;

            Debug.LogWarning($"[GamePaymentSDK] {message}");
        }

        public static void LogError(string message)
        {
            if (!_enabled)
                return;

            Debug.LogError($"[GamePaymentSDK] {message}");
        }
    }
}