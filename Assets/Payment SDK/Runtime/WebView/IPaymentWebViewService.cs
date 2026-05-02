using System;

namespace GamePaymentSDK.WebView
{
    public interface IPaymentWebViewService
    {
        event Action<string> UrlChanged;
        event Action ClosedByUser;
        event Action<string> LoadFailed;

        void Open(string url);
        void Close();
    }
}