using System;
using UnityEngine;

namespace GamePaymentSDK.WebView
{
    public interface IPaymentWebViewService
    {
        event Action<string> UrlChanged;
        event Action ClosedByUser;
        event Action<string> LoadFailed;

        ILogger Logger { get; set; }

        void Open(string url);
        void Close();
    }
}