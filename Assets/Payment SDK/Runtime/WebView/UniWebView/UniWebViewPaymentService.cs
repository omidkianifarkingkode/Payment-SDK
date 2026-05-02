using System;
using GamePaymentSDK.Core;
using UnityEngine;

namespace GamePaymentSDK.WebView.UniWebViewAdapter
{
    public sealed class UniWebViewPaymentService : MonoBehaviour, IPaymentWebViewService
    {
        public event Action<string> UrlChanged;
        public event Action ClosedByUser;
        public event Action<string> LoadFailed;

        [SerializeField] private UniWebViewPaymentSettings _settings = new();

        private UniWebView _webView;
        private bool _isOpen;
        private bool _isClosingBySdk;

        public bool IsOpen => _isOpen;

        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Payment URL is null or empty.", nameof(url));

            CloseInternal(raiseClosedByUser: false);

            CreateWebView();

            _isOpen = true;
            _isClosingBySdk = false;

            PaymentLogger.Log($"Opening UniWebView payment URL: {url}");

            _webView.Load(url);

            if (_settings.AnimatedShow)
            {
                _webView.Show(
                    fade: true,
                    edge: UniWebViewTransitionEdge.Bottom,
                    duration: _settings.ShowDuration
                );
            }
            else
            {
                _webView.Show();
            }
        }

        public void Close()
        {
            CloseInternal(raiseClosedByUser: false);
        }

        private void CreateWebView()
        {
            _webView = gameObject.AddComponent<UniWebView>();

            ApplyLayout();
            ApplyBehaviorSettings();
            RegisterEvents();
        }

        private void ApplyLayout()
        {
            if (_webView == null)
                return;

            Rect safeArea = Screen.safeArea;

            int left = Mathf.RoundToInt(safeArea.xMin) + (_settings.SafeAreaPadding?.left ?? 0);
            int top = Mathf.RoundToInt(Screen.height - safeArea.yMax) + (_settings.SafeAreaPadding?.top ?? 0);
            int width = Mathf.RoundToInt(safeArea.width) - ((_settings.SafeAreaPadding?.left ?? 0) + (_settings.SafeAreaPadding?.right ?? 0));
            int height = Mathf.RoundToInt(safeArea.height) - ((_settings.SafeAreaPadding?.top ?? 0) + (_settings.SafeAreaPadding?.bottom ?? 0));

            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            _webView.Frame = new Rect(left, top, width, height);
        }

        private void ApplyBehaviorSettings()
        {
            if (_webView == null)
                return;

            _webView.SetBackButtonEnabled(_settings.EnableBackButton);

            if (_settings.ShowSpinnerWhileLoading)
                _webView.SetShowSpinnerWhileLoading(true);

#if UNITY_IOS
            _webView.SetShowToolbar(
                _settings.ShowToolbarOnIOS,
                animated: true,
                onTop: false,
                adjustInset: true
            );

            _webView.SetToolbarDoneButtonText(_settings.DoneButtonText);
            _webView.SetShowToolbarNavigationButtons(_settings.ShowToolbarNavigationButtons);
#endif


            // /*
            //  * UniWebView v6:
            //  * EmbeddedToolbar works on iOS, Android, and macOS Editor.
            //  * The Done button triggers OnShouldClose.
            //  */
            // if (_settings.ShowToolbarOnIOS)
            // {
            //     _webView.EmbeddedToolbar.SetDoneButtonText(_settings.DoneButtonText);
            //     _webView.EmbeddedToolbar.Show();
            // }
        }

        private void RegisterEvents()
        {
            if (_webView == null)
                return;

            _webView.OnPageStarted += HandlePageStarted;
            _webView.OnPageFinished += HandlePageFinished;
            _webView.OnLoadingErrorReceived += HandleLoadingErrorReceived;
            _webView.OnShouldClose += HandleShouldClose;
            _webView.OnOrientationChanged += HandleOrientationChanged;
            _webView.OnWebContentProcessTerminated += HandleWebContentProcessTerminated;
        }

        private void UnregisterEvents()
        {
            if (_webView == null)
                return;

            _webView.OnPageStarted -= HandlePageStarted;
            _webView.OnPageFinished -= HandlePageFinished;
            _webView.OnLoadingErrorReceived -= HandleLoadingErrorReceived;
            _webView.OnShouldClose -= HandleShouldClose;
            _webView.OnOrientationChanged -= HandleOrientationChanged;
            _webView.OnWebContentProcessTerminated -= HandleWebContentProcessTerminated;
        }

        private void HandlePageStarted(UniWebView webView, string url)
        {
            PaymentLogger.Log($"UniWebView page started: {url}");
            UrlChanged?.Invoke(url);
        }

        private void HandlePageFinished(UniWebView webView, int statusCode, string url)
        {
            PaymentLogger.Log($"UniWebView page finished: status={statusCode}, url={url}");
            UrlChanged?.Invoke(url);
        }

        private void HandleLoadingErrorReceived(
            UniWebView webView,
            int errorCode,
            string errorMessage,
            UniWebViewNativeResultPayload payload
        )
        {
            string failingUrl = null;

            try
            {
                if (payload != null &&
                    payload.Extra != null &&
                    payload.Extra.ContainsKey(UniWebViewNativeResultPayload.ExtraFailingURLKey))
                {
                    object value = payload.Extra[UniWebViewNativeResultPayload.ExtraFailingURLKey];
                    failingUrl = value?.ToString();
                }
            }
            catch
            {
                failingUrl = null;
            }

            string message = string.IsNullOrWhiteSpace(failingUrl)
                ? $"UniWebView loading error. code={errorCode}, message={errorMessage}"
                : $"UniWebView loading error. code={errorCode}, message={errorMessage}, url={failingUrl}";

            PaymentLogger.LogWarning(message);

            LoadFailed?.Invoke(message);
        }

        private bool HandleShouldClose(UniWebView webView)
        {
            PaymentLogger.Log("UniWebView requested close.");

            bool wasClosedBySdk = _isClosingBySdk;

            CloseInternal(raiseClosedByUser: !wasClosedBySdk);

            /*
             * Return true because we already clean our reference and allow UniWebView
             * to finish its own close/destroy flow.
             */
            return true;
        }

        private void HandleOrientationChanged(UniWebView webView, ScreenOrientation orientation)
        {
            ApplyLayout();
        }

        private void HandleWebContentProcessTerminated(UniWebView webView)
        {
            string message = "UniWebView web content process terminated.";
            PaymentLogger.LogWarning(message);
            LoadFailed?.Invoke(message);
            CloseInternal(raiseClosedByUser: false);
        }

        private void CloseInternal(bool raiseClosedByUser)
        {
            if (_webView == null)
            {
                _isOpen = false;
                _isClosingBySdk = false;
                return;
            }

            UniWebView webViewToDestroy = _webView;

            _isClosingBySdk = !raiseClosedByUser;

            UnregisterEvents();

            try
            {
                webViewToDestroy.Hide();
            }
            catch
            {
                // Ignore hide errors during cleanup.
            }

            try
            {
                Destroy(webViewToDestroy);
            }
            catch
            {
                // Ignore destroy errors during cleanup.
            }

            _webView = null;
            _isOpen = false;

            if (raiseClosedByUser)
                ClosedByUser?.Invoke();

            _isClosingBySdk = false;
        }

        private void OnDestroy()
        {
            CloseInternal(raiseClosedByUser: false);
        }
    }
}