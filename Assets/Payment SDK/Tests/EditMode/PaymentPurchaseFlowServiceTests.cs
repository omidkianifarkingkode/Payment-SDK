using System.Collections.Generic;
using GamePaymentSDK.Core;
using GamePaymentSDK.Services;
using GamePaymentSDK.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace GamePaymentSDK.Tests.EditMode
{
    /// <summary>
    /// Drives the purchase orchestration end-to-end with fake collaborators.
    /// The FakeWebViewService raises its events synchronously on Open, so the
    /// async flow resolves immediately and can be awaited synchronously.
    /// </summary>
    public sealed class PaymentPurchaseFlowServiceTests
    {
        private const string ClientId = "client-123";
        private const string PlayerId = "player-1";
        private const string OrderId = "ZPR_1";
        private const string ProductKey = "gem_small";

        private PaymentConfiguration _config;
        private FakePaymentRequestService _requestService;
        private FakePaymentClaimService _claimService;
        private FakeWebViewService _webView;
        private PaymentCallbackParser _parser;

        [SetUp]
        public void SetUp()
        {
            _config = new PaymentConfiguration
            {
                BaseUrl = "https://api.example.com",
                ApiKey = "key",
                WebViewTimeoutSeconds = 300
            };

            _requestService = new FakePaymentRequestService
            {
                RequestResult = PaymentResult<PaymentStartResult>.Ok(new PaymentStartResult
                {
                    OrderId = OrderId,
                    PlayerId = PlayerId,
                    ProductKey = ProductKey,
                    PaymentUrl = "https://pay.example.com/x"
                })
            };

            _claimService = new FakePaymentClaimService();
            _webView = new FakeWebViewService();
            _parser = new PaymentCallbackParser(_config, UnityEngine.Debug.unityLogger);
        }

        private PaymentPurchaseFlowService CreateFlow()
        {
            return new PaymentPurchaseFlowService(
                _config,
                _requestService,
                _claimService,
                _webView,
                _parser,
                UnityEngine.Debug.unityLogger
            );
        }

        private static string CallbackUrl(string status)
        {
            return $"https://api.example.com/v1/payments/callback/{ClientId}?authority=A1&status={status}";
        }

        private static PaymentResult<List<PaymentPurchaseResult>> OnePurchase()
        {
            return PaymentResult<List<PaymentPurchaseResult>>.Ok(new List<PaymentPurchaseResult>
            {
                new PaymentPurchaseResult { OrderId = OrderId, ProductKey = ProductKey }
            });
        }

        private static PaymentResult<List<PaymentPurchaseResult>> NoPurchases()
        {
            return PaymentResult<List<PaymentPurchaseResult>>.Ok(new List<PaymentPurchaseResult>());
        }

        [Test]
        public void Purchase_CallbackOkAndClaimReturnsItem_Succeeds()
        {
            _webView.EmitUrlOnOpen = CallbackUrl("OK");
            _claimService.ClaimOrderResult = OnePurchase();

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Data.Count);
            Assert.IsTrue(_requestService.CallbackDetectedMarked);
            Assert.AreEqual(1, _requestService.ClearActivePurchaseCallCount);
            Assert.AreEqual(1, _webView.CloseCallCount);
        }

        [Test]
        public void Purchase_CallbackOkButClaimEmpty_FailsWithPaymentNotVerified()
        {
            _webView.EmitUrlOnOpen = CallbackUrl("OK");
            _claimService.ClaimOrderResult = NoPurchases();

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.PaymentNotVerified, result.FailureReason);
        }

        [Test]
        public void Purchase_CallbackNok_FailsWithPaymentCancelledAndMarksFailed()
        {
            _webView.EmitUrlOnOpen = CallbackUrl("NOK");

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.PaymentCancelled, result.FailureReason);
            Assert.IsTrue(_requestService.PurchaseFailedMarked);
            Assert.AreEqual(0, _claimService.ClaimOrderCallCount, "NOK must not attempt a claim");
        }

        [Test]
        public void Purchase_ClosedByUserWithoutRecovery_FailsWithWebViewClosedByUser()
        {
            _webView.EmitClosedByUserOnOpen = true;
            _claimService.ClaimOrderResult = NoPurchases();

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.WebViewClosedByUser, result.FailureReason);
            Assert.AreEqual(1, _claimService.ClaimOrderCallCount, "should attempt one recovery claim");
        }

        [Test]
        public void Purchase_ClosedByUserButRecoveryClaimSucceeds_Succeeds()
        {
            _webView.EmitClosedByUserOnOpen = true;
            _claimService.ClaimOrderResult = OnePurchase();

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Data.Count);
        }

        [Test]
        public void Purchase_LoadFailedWithoutRecovery_FailsWithWebViewOpenFailed()
        {
            _webView.EmitLoadFailedOnOpen = "net error";
            _claimService.ClaimOrderResult = NoPurchases();

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.WebViewOpenFailed, result.FailureReason);
        }

        [Test]
        public void Purchase_OpenThrows_FailsWithWebViewOpenFailed()
        {
            _webView.ThrowOnOpen = true;

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.WebViewOpenFailed, result.FailureReason);
            Assert.IsTrue(_requestService.PurchaseFailedMarked);
        }

        [Test]
        public void Purchase_RequestFails_PropagatesAndNeverOpensWebView()
        {
            _requestService.RequestResult = PaymentResult<PaymentStartResult>.Fail(
                PaymentFailureReason.ServerError, "request boom");

            PaymentResult<List<PaymentPurchaseResult>> result =
                CreateFlow().PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.ServerError, result.FailureReason);
            Assert.AreEqual(0, _webView.OpenCallCount);
        }

        [Test]
        public void Purchase_NullWebViewDependency_FailsWithInvalidConfiguration()
        {
            PaymentPurchaseFlowService flow = new PaymentPurchaseFlowService(
                _config,
                _requestService,
                _claimService,
                null,
                _parser,
                UnityEngine.Debug.unityLogger
            );

            PaymentResult<List<PaymentPurchaseResult>> result =
                flow.PurchaseAsync(PlayerId, ProductKey).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PaymentFailureReason.InvalidConfiguration, result.FailureReason);
        }
    }
}
