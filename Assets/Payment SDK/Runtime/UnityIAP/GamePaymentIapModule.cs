using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;
using UnityEngine;
using UnityEngine.Purchasing.Extension;

namespace GamePaymentSDK.UnityIAP
{
    public sealed class GamePaymentIapModule : AbstractPurchasingModule
    {
        private readonly IPaymentSettings _settings;
        private readonly string _playerId;
        private readonly IPaymentWebViewService _webViewService;
        private readonly ILogger _logger;

        private GamePaymentIapModule(
            IPaymentSettings settings,
            string playerId,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            _settings = settings;
            _playerId = playerId;
            _webViewService = webViewService;
            _logger = logger;
        }

        public static GamePaymentIapModule Instance(
            IPaymentSettings settings,
            string playerId,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            return new GamePaymentIapModule(settings, playerId, webViewService, logger);
        }

        public override void Configure()
        {
            RegisterStore(
                GamePaymentIapStoreConstants.StoreName,
                new GamePaymentIapStore(_settings, _playerId, _webViewService, _logger)
            );
        }
    }
}