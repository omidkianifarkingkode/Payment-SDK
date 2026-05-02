/// Behaviors:
/// # Success path:
/// 1. PurchaseAsync(productKey)
/// 2. POST /v1/payments/request
/// 3. Save PendingOrder
/// 4. Open WebView
/// 5. Detect callback status=OK
/// 6. POST /v1/payments/claim
/// 7. Return PaymentPurchaseResult
/// # User closes WebView:
/// 1. WebView closed by user
/// 2. SDK tries ClaimOrderAsync once
/// 3. If claim returns purchase: success
/// 4. Else: return WebViewClosedByUser and keep local pending order
/// This protects the case where:
/// - User paid successfully but closed WebView before Unity detected callback
/// # Timeout or WebView load failure
/// 1. SDK tries ClaimOrderAsync once
/// 2. If claim succeeds: the purchase still succeed
/// 3. If claim fails: the order remains locally pending for future recovery.
/// # Callback NOK
/// 1. SDK returns: PaymentCancelled
/// 2. Marks the local pending order as: Failed
/// # Callback OK but claim returns empty (Backend has not returned a claimable purchase yet)
/// 1. SDK returns: PaymentNotVerified
/// 2. Keeps the pending order locally
/// 3. Then on next app start : Claim Local Pending Orders (recovering)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamePaymentSDK.Core;
using GamePaymentSDK.WebView;

namespace GamePaymentSDK.Services
{
    public sealed class PaymentPurchaseFlowService : IPaymentPurchaseFlowService
    {
        private readonly PaymentConfiguration _configuration;
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IPaymentWebViewService _webViewService;
        private readonly IPaymentCallbackParser _callbackParser;

        public bool IsPurchaseInProgress =>
            _paymentRequestService != null &&
            _paymentRequestService.IsPurchaseInProgress;

        public PaymentPurchaseFlowService(
            PaymentConfiguration configuration,
            IPaymentRequestService paymentRequestService,
            IPaymentClaimService paymentClaimService,
            IPaymentWebViewService webViewService,
            IPaymentCallbackParser callbackParser
        )
        {
            _configuration = configuration;
            _paymentRequestService = paymentRequestService;
            _paymentClaimService = paymentClaimService;
            _webViewService = webViewService;
            _callbackParser = callbackParser;
        }

        public async Task<PaymentResult<List<PaymentPurchaseResult>>> PurchaseAsync(
            string playerId,
            string productKey
        )
        {
            PaymentResult validation = ValidateDependencies();

            if (!validation.Success)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    validation.FailureReason,
                    validation.ErrorMessage
                );
            }

            PaymentLogger.Log($"Purchase flow started. playerId={playerId}, productKey={productKey}");

            PaymentResult<PaymentStartResult> startResult =
                await _paymentRequestService.RequestPaymentAsync(playerId, productKey);

            if (!startResult.Success)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    startResult.FailureReason,
                    startResult.ErrorMessage
                );
            }

            PaymentStartResult paymentStart = startResult.Data;

            PaymentWebViewFlowResult webViewResult =
                await OpenPaymentWebViewAndWaitAsync(paymentStart);

            PaymentResult<List<PaymentPurchaseResult>> finalResult =
                await HandleWebViewResultAsync(paymentStart, webViewResult);

            _paymentRequestService.ClearActivePurchase();

            if (finalResult.Success)
            {
                PaymentLogger.Log(
                    $"Purchase flow succeeded. orderId={paymentStart.OrderId}, productKey={paymentStart.ProductKey}, count={finalResult.Data.Count}"
                );
            }
            else
            {
                PaymentLogger.LogWarning(
                    $"Purchase flow failed. orderId={paymentStart.OrderId}, productKey={paymentStart.ProductKey}, reason={finalResult.FailureReason}, error={finalResult.ErrorMessage}"
                );
            }

            return finalResult;
        }

        private async Task<PaymentWebViewFlowResult> OpenPaymentWebViewAndWaitAsync(
            PaymentStartResult paymentStart
        )
        {
            TaskCompletionSource<PaymentWebViewFlowResult> completion = new();

            void OnUrlChanged(string url)
            {
                PaymentCallbackResult callback = _callbackParser.Parse(url);

                if (!callback.IsPaymentCallback)
                    return;

                PaymentLogger.Log(
                    $"Payment callback detected. orderId={paymentStart.OrderId}, status={callback.StatusRaw}, authority={callback.Authority}"
                );

                completion.TrySetResult(new PaymentWebViewFlowResult
                {
                    CompletionType = PaymentWebViewCompletionType.CallbackDetected,
                    CallbackResult = callback
                });
            }

            void OnClosedByUser()
            {
                completion.TrySetResult(new PaymentWebViewFlowResult
                {
                    CompletionType = PaymentWebViewCompletionType.ClosedByUser,
                    ErrorMessage = "WebView closed by user."
                });
            }

            void OnLoadFailed(string error)
            {
                completion.TrySetResult(new PaymentWebViewFlowResult
                {
                    CompletionType = PaymentWebViewCompletionType.LoadFailed,
                    ErrorMessage = error
                });
            }

            _webViewService.UrlChanged += OnUrlChanged;
            _webViewService.ClosedByUser += OnClosedByUser;
            _webViewService.LoadFailed += OnLoadFailed;

            try
            {
                _paymentRequestService.MarkWebViewOpened(paymentStart.OrderId);

                try
                {
                    _webViewService.Open(paymentStart.PaymentUrl);
                }
                catch (Exception exception)
                {
                    return new PaymentWebViewFlowResult
                    {
                        CompletionType = PaymentWebViewCompletionType.OpenFailed,
                        ErrorMessage = exception.Message
                    };
                }

                int timeoutSeconds = GetWebViewTimeoutSeconds();

                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

                Task completedTask = await Task.WhenAny(completion.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    TryCloseWebView();

                    return new PaymentWebViewFlowResult
                    {
                        CompletionType = PaymentWebViewCompletionType.Timeout,
                        ErrorMessage = $"WebView timed out after {timeoutSeconds} seconds."
                    };
                }

                PaymentWebViewFlowResult result = await completion.Task;

                if (result.CompletionType == PaymentWebViewCompletionType.CallbackDetected)
                    TryCloseWebView();

                return result;
            }
            finally
            {
                _webViewService.UrlChanged -= OnUrlChanged;
                _webViewService.ClosedByUser -= OnClosedByUser;
                _webViewService.LoadFailed -= OnLoadFailed;
            }
        }

        private async Task<PaymentResult<List<PaymentPurchaseResult>>> HandleWebViewResultAsync(
            PaymentStartResult paymentStart,
            PaymentWebViewFlowResult webViewResult
        )
        {
            if (webViewResult == null)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.Unknown,
                    "WebView result is null."
                );
            }

            switch (webViewResult.CompletionType)
            {
                case PaymentWebViewCompletionType.CallbackDetected:
                    return await HandleCallbackDetectedAsync(paymentStart, webViewResult.CallbackResult);

                case PaymentWebViewCompletionType.ClosedByUser:
                    return await HandleWebViewClosedByUserAsync(paymentStart);

                case PaymentWebViewCompletionType.LoadFailed:
                    return await HandleRecoverableWebViewFailureAsync(
                        paymentStart,
                        PaymentFailureReason.WebViewOpenFailed,
                        webViewResult.ErrorMessage
                    );

                case PaymentWebViewCompletionType.Timeout:
                    return await HandleRecoverableWebViewFailureAsync(
                        paymentStart,
                        PaymentFailureReason.WebViewTimeout,
                        webViewResult.ErrorMessage
                    );

                case PaymentWebViewCompletionType.OpenFailed:
                    _paymentRequestService.MarkPurchaseFailed(paymentStart.OrderId);

                    return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                        PaymentFailureReason.WebViewOpenFailed,
                        webViewResult.ErrorMessage
                    );

                default:
                    return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                        PaymentFailureReason.Unknown,
                        webViewResult.ErrorMessage
                    );
            }
        }

        private async Task<PaymentResult<List<PaymentPurchaseResult>>> HandleCallbackDetectedAsync(
            PaymentStartResult paymentStart,
            PaymentCallbackResult callback
        )
        {
            if (callback == null || !callback.IsPaymentCallback)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.PaymentCallbackFailed,
                    "Invalid payment callback."
                );
            }

            _paymentRequestService.MarkCallbackDetected(paymentStart.OrderId);

            if (!callback.IsSuccess)
            {
                _paymentRequestService.MarkPurchaseFailed(paymentStart.OrderId);

                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.PaymentCancelled,
                    $"Payment callback status is not OK. status={callback.StatusRaw}"
                );
            }

            PaymentResult<List<PaymentPurchaseResult>> claimResult =
                await _paymentClaimService.ClaimOrderAsync(
                    paymentStart.PlayerId,
                    paymentStart.OrderId
                );

            if (!claimResult.Success)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    claimResult.FailureReason,
                    claimResult.ErrorMessage
                );
            }

            if (claimResult.Data == null || claimResult.Data.Count == 0)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                    PaymentFailureReason.PaymentNotVerified,
                    "Payment callback was successful, but claim returned no purchase. The order remains pending and can be recovered on next app start."
                );
            }

            return PaymentResult<List<PaymentPurchaseResult>>.Ok(claimResult.Data);
        }

        private async Task<PaymentResult<List<PaymentPurchaseResult>>> HandleWebViewClosedByUserAsync(
            PaymentStartResult paymentStart
        )
        {
            /*
             * User may close the WebView after payment but before Unity detects the callback.
             * So we try one claim before returning cancellation.
             */
            PaymentResult<List<PaymentPurchaseResult>> recoveryClaim =
                await _paymentClaimService.ClaimOrderAsync(
                    paymentStart.PlayerId,
                    paymentStart.OrderId
                );

            if (recoveryClaim.Success &&
                recoveryClaim.Data != null &&
                recoveryClaim.Data.Count > 0)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Ok(recoveryClaim.Data);
            }

            return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                PaymentFailureReason.WebViewClosedByUser,
                "WebView was closed by user. Order remains locally pending for future recovery."
            );
        }

        private async Task<PaymentResult<List<PaymentPurchaseResult>>> HandleRecoverableWebViewFailureAsync(
            PaymentStartResult paymentStart,
            PaymentFailureReason fallbackReason,
            string errorMessage
        )
        {
            /*
             * A load failure or timeout may happen after payment was completed.
             * Try claim once before failing.
             */
            PaymentResult<List<PaymentPurchaseResult>> recoveryClaim =
                await _paymentClaimService.ClaimOrderAsync(
                    paymentStart.PlayerId,
                    paymentStart.OrderId
                );

            if (recoveryClaim.Success &&
                recoveryClaim.Data != null &&
                recoveryClaim.Data.Count > 0)
            {
                return PaymentResult<List<PaymentPurchaseResult>>.Ok(recoveryClaim.Data);
            }

            return PaymentResult<List<PaymentPurchaseResult>>.Fail(
                fallbackReason,
                errorMessage
            );
        }

        private PaymentResult ValidateDependencies()
        {
            if (_configuration == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "PaymentConfiguration is null."
                );
            }

            if (_paymentRequestService == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentRequestService is null."
                );
            }

            if (_paymentClaimService == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentClaimService is null."
                );
            }

            if (_webViewService == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentWebViewService is null."
                );
            }

            if (_callbackParser == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentCallbackParser is null."
                );
            }

            return PaymentResult.Ok();
        }

        private int GetWebViewTimeoutSeconds()
        {
            if (_configuration.WebViewTimeoutSeconds <= 0)
                return 300;

            return _configuration.WebViewTimeoutSeconds;
        }

        private void TryCloseWebView()
        {
            try
            {
                _webViewService.Close();
            }
            catch (Exception exception)
            {
                PaymentLogger.LogWarning($"Failed to close WebView. {exception.Message}");
            }
        }
    }
}