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

        [SerializeField] private IPaymentSettings _settings;
        [SerializeField] private bool _autoComplete = true;
        [SerializeField] private bool _autoSuccess = true;
        [SerializeField] private float _autoCompleteDelaySeconds = 2f;

        private Coroutine _autoCompleteRoutine;
        private string _lastOpenedUrl;

        public ILogger Logger { get; set; }

        public void SetSettings(IPaymentSettings settings)
        {
            _settings = settings;
        }

        public void Open(string url)
        {
            _lastOpenedUrl = url;
            Application.OpenURL(url);

            Logger.Log(LogType.Log, $"[PaymentSdk] [MockPaymentWebViewService] Mock WebView opened: {url}");

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
            Logger.Log(LogType.Log, $"[PaymentSdk] [MockPaymentWebViewService] Mock WebView closed by SDK.");

            StopAutoCompleteRoutine();
        }

        public void SimulateSuccess()
        {
            string callbackUrl = BuildCallbackUrl("OK");
            Logger.Log(LogType.Log, $"[PaymentSdk] [MockPaymentWebViewService] Mock WebView simulate success: {callbackUrl}");
            UrlChanged?.Invoke(callbackUrl);
        }

        public void SimulateCancel()
        {
            string callbackUrl = BuildCallbackUrl("NOK");
            Logger.Log(LogType.Log, $"[PaymentSdk] [MockPaymentWebViewService] Mock WebView simulate cancel: {callbackUrl}");
            UrlChanged?.Invoke(callbackUrl);
        }

        public void SimulateUserClose()
        {
            Logger.Log(LogType.Log, $"[PaymentSdk] [MockPaymentWebViewService] Mock WebView simulate user close.");
            StopAutoCompleteRoutine();
            ClosedByUser?.Invoke();
        }

        public void SimulateLoadFailed(string message = "Mock WebView load failed.")
        {
            Logger.Log(LogType.Warning, message);
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
            string baseUrl = _settings != null
                ? _settings.BaseUrl
                : "https://mock-payment.local";

            string authority = ExtractMockAuthority(_lastOpenedUrl);

            return $"{baseUrl}/v1/payments/callback/clientId?authority={authority}&status={status}";
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