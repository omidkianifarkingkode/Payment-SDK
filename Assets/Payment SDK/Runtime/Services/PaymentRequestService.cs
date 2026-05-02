/// Important Behavior:
/// # Preventing double purchase flow
/// If player taps purchase button many times:
/// First tap: request starts
/// Second tap: PurchaseAlreadyInProgress
/// This is important because for Phase 1, the safest rule is:
/// Only one active purchase WebView session at a time.
/// -----------------
/// # Storing pending order immediately
/// As soon as backend creates the order:
/// POST /v1/payments/request success
/// The SDK saves:
/// orderId, playerId, productKey, paymentUrl, createdAt, status = PaymentUrlReceived
/// So if the app crashes after getting paymentUrl, you still have recovery data.
/// -----------------
/// # Not removing failed orders automatically
/// When status becomes: Failed -> The order remains in local storage for now.
/// Reason: During development and QA, keeping failed local records helps debugging.
/// Later we can add cleanup rules like:
/// - Remove failed orders older than 24 hours
/// - Remove expired local orders older than 7 days

using System.Threading.Tasks;
using GamePaymentSDK.Api;
using GamePaymentSDK.Core;
using GamePaymentSDK.Storage;

namespace GamePaymentSDK.Services
{
    public sealed class PaymentRequestService : IPaymentRequestService
    {
        private readonly IPaymentApiClient _apiClient;
        private readonly IProductCatalogService _productCatalogService;
        private readonly IPendingOrderStorage _pendingOrderStorage;

        private bool _isPurchaseInProgress;
        private string _activeOrderId;

        public bool IsPurchaseInProgress => _isPurchaseInProgress;
        public string ActiveOrderId => _activeOrderId;

        public PaymentRequestService(
            IPaymentApiClient apiClient,
            IProductCatalogService productCatalogService,
            IPendingOrderStorage pendingOrderStorage
        )
        {
            _apiClient = apiClient;
            _productCatalogService = productCatalogService;
            _pendingOrderStorage = pendingOrderStorage;
        }

        public async Task<PaymentResult<PaymentStartResult>> RequestPaymentAsync(
            string playerId,
            string productKey
        )
        {
            PaymentResult validationResult = ValidateRequest(playerId, productKey);

            if (!validationResult.Success)
            {
                return PaymentResult<PaymentStartResult>.Fail(
                    validationResult.FailureReason,
                    validationResult.ErrorMessage
                );
            }

            if (_isPurchaseInProgress)
            {
                return PaymentResult<PaymentStartResult>.Fail(
                    PaymentFailureReason.PurchaseAlreadyInProgress,
                    $"Another purchase is already in progress. activeOrderId={_activeOrderId}"
                );
            }

            _isPurchaseInProgress = true;
            _activeOrderId = null;

            PaymentLogger.Log($"Payment request started. playerId={playerId}, productKey={productKey}");

            PaymentResult<PaymentRequestResponseDto> apiResult =
                await _apiClient.RequestPaymentAsync(playerId, productKey);

            if (!apiResult.Success)
            {
                _isPurchaseInProgress = false;
                _activeOrderId = null;

                PaymentLogger.LogWarning(
                    $"Payment request failed. productKey={productKey}, reason={apiResult.FailureReason}, error={apiResult.ErrorMessage}"
                );

                return PaymentResult<PaymentStartResult>.Fail(
                    apiResult.FailureReason,
                    apiResult.ErrorMessage
                );
            }

            PaymentRequestResponseDto response = apiResult.Data;

            PendingOrder pendingOrder = new PendingOrder
            {
                OrderId = response.orderId,
                PlayerId = playerId,
                ProductKey = productKey,
                PaymentUrl = response.paymentUrl,
                CreatedAtUnixSeconds = UnixTime.NowSeconds(),
                ClaimAttemptCount = 0,
                Status = PendingOrderStatus.PaymentUrlReceived
            };

            _pendingOrderStorage.Save(pendingOrder);

            _activeOrderId = response.orderId;

            PaymentStartResult result = new PaymentStartResult
            {
                OrderId = response.orderId,
                PlayerId = playerId,
                ProductKey = productKey,
                PaymentUrl = response.paymentUrl
            };

            PaymentLogger.Log(
                $"Payment request completed. orderId={result.OrderId}, productKey={result.ProductKey}"
            );

            return PaymentResult<PaymentStartResult>.Ok(result);
        }

        public void ClearActivePurchase()
        {
            PaymentLogger.Log($"Clear active purchase. activeOrderId={_activeOrderId}");

            _isPurchaseInProgress = false;
            _activeOrderId = null;
        }

        public void MarkWebViewOpened(string orderId)
        {
            UpdatePendingOrderStatus(orderId, PendingOrderStatus.WebViewOpened);
        }

        public void MarkCallbackDetected(string orderId)
        {
            UpdatePendingOrderStatus(orderId, PendingOrderStatus.CallbackDetected);
        }

        public void MarkPurchaseFailed(string orderId)
        {
            UpdatePendingOrderStatus(orderId, PendingOrderStatus.Failed);

            if (_activeOrderId == orderId)
                ClearActivePurchase();
        }

        private PaymentResult ValidateRequest(string playerId, string productKey)
        {
            if (_apiClient == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPaymentApiClient is null."
                );
            }

            if (_productCatalogService == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IProductCatalogService is null."
                );
            }

            if (_pendingOrderStorage == null)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidConfiguration,
                    "IPendingOrderStorage is null."
                );
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            if (string.IsNullOrWhiteSpace(productKey))
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.ProductNotFound,
                    "productKey is required."
                );
            }

            if (!_productCatalogService.IsReady)
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.NotInitialized,
                    "Product catalog is not ready."
                );
            }

            if (!_productCatalogService.TryGetProduct(productKey, out PaymentProduct _))
            {
                return PaymentResult.Fail(
                    PaymentFailureReason.ProductUnavailable,
                    $"Product not found in RAM catalog. productKey={productKey}"
                );
            }

            return PaymentResult.Ok();
        }

        private void UpdatePendingOrderStatus(string orderId, string status)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return;

            if (!_pendingOrderStorage.TryGet(orderId, out PendingOrder order))
            {
                PaymentLogger.LogWarning(
                    $"Cannot update pending order status. Order not found. orderId={orderId}, status={status}"
                );

                return;
            }

            order.Status = status;
            _pendingOrderStorage.Save(order);

            PaymentLogger.Log(
                $"Pending order status updated. orderId={orderId}, status={status}"
            );
        }
    }
}