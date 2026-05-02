using System;
using UnityEngine;

namespace GamePaymentSDK.WebView.UniWebViewAdapter
{
    [Serializable]
    public sealed class UniWebViewPaymentSettings
    {
        [Header("Layout")]
        public RectOffset SafeAreaPadding;

        [Header("Behavior")]
        public bool ShowSpinnerWhileLoading = true;
        public bool EnableBackButton = true;
        public bool ShowToolbarOnIOS = true;
        public bool ShowToolbarNavigationButtons = false;

        [Header("Toolbar")]
        public string DoneButtonText = "Close";

        [Header("Animation")]
        public bool AnimatedShow = true;
        public float ShowDuration = 0.25f;
    }
}