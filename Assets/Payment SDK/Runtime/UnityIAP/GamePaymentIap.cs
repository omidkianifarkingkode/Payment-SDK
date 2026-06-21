using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;
using System;
using UnityEngine;

namespace GamePaymentSDK.UnityIAP
{
    public static class GamePaymentIap
    {
        private static IPaymentSettings _settings;
        private static IPaymentWebViewService _webViewService;
        private static ILogger _logger;

        internal static void Setup(
            IPaymentSettings settings,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            _settings = settings;
            _webViewService = webViewService;
            _logger = logger;

            if (_webViewService != null)
                _webViewService.Logger = logger;
        }

        /// <summary>
        /// Creates a configured <see cref="GamePaymentIapModule"/> for use with Unity IAP's
        /// <c>ConfigurationBuilder.Instance(module, ...)</c>.
        /// Requires <see cref="Setup"/> to have been called first (handled by <see cref="PaymentIapBootstrap"/>).
        /// </summary>
        public static GamePaymentIapModule CreateModule(string playerId)
        {
            if (_settings == null)
                throw new InvalidOperationException(
                    "[PaymentSdk] Call GamePaymentIap.Setup (via PaymentIapBootstrap) before CreateModule.");

            return GamePaymentIapModule.Instance(_settings, playerId, _webViewService, _logger);
        }
    }
}
