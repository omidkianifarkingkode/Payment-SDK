using System;
using System.Collections;
using GamePaymentSDK.Core;
using UnityEngine;

namespace GamePaymentSDK.WebView.Mock
{
    public sealed class MockPaymentWebViewService : MonoBehaviour, IPaymentWebViewService
    {
        public event Action<string> UrlChanged;
        public event Action ClosedByUser;
        public event Action<string> LoadFailed;

        [SerializeField] private PaymentConfiguration _configuration;
        [SerializeField] private bool _autoComplete = true;
        [SerializeField] private bool _autoSuccess = true;
        [SerializeField] private float _autoCompleteDelaySeconds = 2f;

        private Coroutine _autoCompleteRoutine;
        private string _lastOpenedUrl;

        public void SetConfiguration(PaymentConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Open(string url)
        {
            _lastOpenedUrl = url;

            PaymentLogger.Log($"Mock WebView opened: {url}");

            UrlChanged?.Invoke(url);

            if (_autoComplete)
            {
                if (_autoCompleteRoutine != null)
                    StopCoroutine(_autoCompleteRoutine);

                _autoCompleteRoutine = StartCoroutine(AutoCompleteRoutine());
            }
        }

        public void Close()
        {
            PaymentLogger.Log("Mock WebView closed by SDK.");

            StopAutoCompleteRoutine();
        }

        public void SimulateSuccess()
        {
            string callbackUrl = BuildCallbackUrl("OK");
            PaymentLogger.Log($"Mock WebView simulate success: {callbackUrl}");
            UrlChanged?.Invoke(callbackUrl);
        }

        public void SimulateCancel()
        {
            string callbackUrl = BuildCallbackUrl("NOK");
            PaymentLogger.Log($"Mock WebView simulate cancel: {callbackUrl}");
            UrlChanged?.Invoke(callbackUrl);
        }

        public void SimulateUserClose()
        {
            PaymentLogger.Log("Mock WebView simulate user close.");
            StopAutoCompleteRoutine();
            ClosedByUser?.Invoke();
        }

        public void SimulateLoadFailed(string message = "Mock WebView load failed.")
        {
            PaymentLogger.LogWarning(message);
            StopAutoCompleteRoutine();
            LoadFailed?.Invoke(message);
        }

        private IEnumerator AutoCompleteRoutine()
        {
            yield return new WaitForSeconds(_autoCompleteDelaySeconds);

            if (_autoSuccess)
                SimulateSuccess();
            else
                SimulateCancel();

            _autoCompleteRoutine = null;
        }

        private void StopAutoCompleteRoutine()
        {
            if (_autoCompleteRoutine != null)
            {
                StopCoroutine(_autoCompleteRoutine);
                _autoCompleteRoutine = null;
            }
        }

        private string BuildCallbackUrl(string status)
        {
            string baseUrl = _configuration != null
                ? _configuration.GetNormalizedBaseUrl()
                : "https://mock-payment.local";

            string clientId = _configuration != null
                ? _configuration.ClientId
                : "client_mock";

            string authority = ExtractMockAuthority(_lastOpenedUrl);

            return $"{baseUrl}/v1/payments/callback/{clientId}?authority={authority}&status={status}";
        }

        private string ExtractMockAuthority(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "A000MOCK";

            int index = url.LastIndexOf("/", StringComparison.Ordinal);

            if (index < 0 || index >= url.Length - 1)
                return "A000MOCK";

            return url.Substring(index + 1);
        }

        private void OnDestroy()
        {
            StopAutoCompleteRoutine();
        }
    }
}