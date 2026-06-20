using System;
using GamePaymentSDK.WebView;

namespace GamePaymentSDK.Tests.EditMode.Fakes
{
    /// <summary>
    /// Stub <see cref="IPaymentWebViewService"/>. On <see cref="Open"/> it
    /// synchronously raises whichever event the test configured, so the
    /// purchase flow completes deterministically without a real WebView.
    /// </summary>
    public sealed class FakeWebViewService : IPaymentWebViewService
    {
        public event Action<string> UrlChanged;
        public event Action ClosedByUser;
        public event Action<string> LoadFailed;

        public string EmitUrlOnOpen { get; set; }
        public bool EmitClosedByUserOnOpen { get; set; }
        public string EmitLoadFailedOnOpen { get; set; }
        public bool ThrowOnOpen { get; set; }

        public int OpenCallCount { get; private set; }
        public int CloseCallCount { get; private set; }

        public void Open(string url)
        {
            OpenCallCount++;

            if (ThrowOnOpen)
                throw new InvalidOperationException("Fake open failure.");

            if (!string.IsNullOrEmpty(EmitUrlOnOpen))
                UrlChanged?.Invoke(EmitUrlOnOpen);

            if (EmitClosedByUserOnOpen)
                ClosedByUser?.Invoke();

            if (!string.IsNullOrEmpty(EmitLoadFailedOnOpen))
                LoadFailed?.Invoke(EmitLoadFailedOnOpen);
        }

        public void Close()
        {
            CloseCallCount++;
        }
    }
}
