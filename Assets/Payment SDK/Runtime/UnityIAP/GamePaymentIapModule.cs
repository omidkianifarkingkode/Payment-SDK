using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace GamePaymentSDK.UnityIAP
{
    public sealed class GamePaymentIapModule : AbstractPurchasingModule
    {
        private readonly PaymentConfiguration _configuration;
        private readonly string _playerId;
        private readonly IPaymentWebViewService _webViewService;

        private GamePaymentIapModule(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService
        )
        {
            _configuration = configuration;
            _playerId = playerId;
            _webViewService = webViewService;
        }

        public static GamePaymentIapModule Instance(
            PaymentConfiguration configuration,
            string playerId,
            IPaymentWebViewService webViewService
        )
        {
            return new GamePaymentIapModule(
                configuration,
                playerId,
                webViewService
            );
        }

        public override void Configure()
        {
            RegisterStore(
                GamePaymentIapStoreConstants.StoreName,
                new GamePaymentIapStore(
                    _configuration,
                    _playerId,
                    _webViewService
                )
            );
        }
    }
}