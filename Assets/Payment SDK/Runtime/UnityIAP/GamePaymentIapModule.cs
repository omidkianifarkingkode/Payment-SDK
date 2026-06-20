using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine;

namespace GamePaymentSDK.UnityIAP
{
    public sealed class GamePaymentIapModule : AbstractPurchasingModule
    {
        private readonly PaymentConfiguration _configuration;
        private readonly string _playerId;
        private readonly IPaymentWebViewService _webViewService;
        private readonly ILogger _logger;

        private GamePaymentIapModule(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            _configuration = configuration;
            _playerId = playerId;
            _webViewService = webViewService;
            _logger = logger;
        }

        public static GamePaymentIapModule Instance(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService,
            ILogger logger)
        {
            return new GamePaymentIapModule(
                configuration,
                playerId,
                webViewService,
                logger
            );
        }

        public override void Configure()
        {
            RegisterStore(
                GamePaymentIapStoreConstants.StoreName,
                new GamePaymentIapStore(
                    _configuration,
                    _playerId,
                    _webViewService,
                    _logger
                )
            );
        }
    }
}