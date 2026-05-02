using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GamePaymentSDK.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace GamePaymentSDK.Api
{
    public sealed class PaymentApiClient : IPaymentApiClient
    {
        private const string HeaderApiKey = "x-api-key";
        private const string HeaderContentType = "Content-Type";
        private const string HeaderAccept = "Accept";
        private const string JsonContentType = "application/json";

        private readonly PaymentConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly bool _isValid;
        private readonly string _configError;

        public PaymentApiClient(PaymentConfiguration configuration)
        {
            _configuration = configuration;

            if (_configuration == null)
            {
                _isValid = false;
                _configError = "PaymentConfiguration is null.";
                _baseUrl = string.Empty;
                return;
            }

            PaymentLogger.SetEnabled(_configuration.EnableLogs);

            _isValid = _configuration.IsValid(out _configError);
            _baseUrl = _configuration.GetNormalizedBaseUrl();
        }

        public async Task<PaymentResult<List<PaymentProduct>>> GetProductsAsync()
        {
            if (!EnsureValid(out PaymentResult<List<PaymentProduct>> invalidResult))
                return invalidResult;

            string url = $"{_baseUrl}/v1/products";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            ApplyCommonHeaders(request);

            PaymentResult<string> response = await SendAsync(request);

            if (!response.Success)
            {
                return PaymentResult<List<PaymentProduct>>.Fail(
                    response.FailureReason,
                    response.ErrorMessage
                );
            }

            ProductsResponseDto dto;

            try
            {
                dto = JsonUtility.FromJson<ProductsResponseDto>(response.Data);
            }
            catch (Exception exception)
            {
                return PaymentResult<List<PaymentProduct>>.Fail(
                    PaymentFailureReason.ServerError,
                    $"Failed to parse products response. {exception.Message}"
                );
            }

            List<PaymentProduct> products = new();

            if (dto?.products != null)
            {
                foreach (ProductDto productDto in dto.products)
                {
                    if (productDto == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(productDto.productKey))
                        continue;

                    products.Add(new PaymentProduct(
                        productDto.productKey,
                        productDto.name,
                        productDto.price,
                        productDto.currency
                    ));
                }
            }

            return PaymentResult<List<PaymentProduct>>.Ok(products);
        }

        public async Task<PaymentResult<PaymentRequestResponseDto>> RequestPaymentAsync(
            string playerId,
            string productKey
        )
        {
            if (!EnsureValid(out PaymentResult<PaymentRequestResponseDto> invalidResult))
                return invalidResult;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PaymentResult<PaymentRequestResponseDto>.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            if (string.IsNullOrWhiteSpace(productKey))
            {
                return PaymentResult<PaymentRequestResponseDto>.Fail(
                    PaymentFailureReason.ProductNotFound,
                    "productKey is required."
                );
            }

            string url = $"{_baseUrl}/v1/payments/request";

            PaymentRequestDto body = new()
            {
                playerId = playerId,
                productKey = productKey
            };

            string json = JsonUtility.ToJson(body);

            using UnityWebRequest request = CreatePostJsonRequest(url, json);
            ApplyCommonHeaders(request);

            PaymentResult<string> response = await SendAsync(request);

            if (!response.Success)
            {
                return PaymentResult<PaymentRequestResponseDto>.Fail(
                    response.FailureReason,
                    response.ErrorMessage
                );
            }

            try
            {
                PaymentRequestResponseDto dto =
                    JsonUtility.FromJson<PaymentRequestResponseDto>(response.Data);

                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.orderId) ||
                    string.IsNullOrWhiteSpace(dto.paymentUrl))
                {
                    return PaymentResult<PaymentRequestResponseDto>.Fail(
                        PaymentFailureReason.ServerError,
                        "Payment request response is missing orderId or paymentUrl."
                    );
                }

                return PaymentResult<PaymentRequestResponseDto>.Ok(dto);
            }
            catch (Exception exception)
            {
                return PaymentResult<PaymentRequestResponseDto>.Fail(
                    PaymentFailureReason.ServerError,
                    $"Failed to parse payment request response. {exception.Message}"
                );
            }
        }

        public async Task<PaymentResult<List<ClaimItemDto>>> ClaimAsync(
            string playerId,
            string orderId = null
        )
        {
            if (!EnsureValid(out PaymentResult<List<ClaimItemDto>> invalidResult))
                return invalidResult;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PaymentResult<List<ClaimItemDto>>.Fail(
                    PaymentFailureReason.InvalidPlayerId,
                    "playerId is required."
                );
            }

            string url = $"{_baseUrl}/v1/payments/claim";

            ClaimRequestDto body = new()
            {
                playerId = playerId,
                orderId = orderId
            };

            string json = JsonUtility.ToJson(body);

            using UnityWebRequest request = CreatePostJsonRequest(url, json);
            ApplyCommonHeaders(request);

            PaymentResult<string> response = await SendAsync(request);

            if (!response.Success)
            {
                return PaymentResult<List<ClaimItemDto>>.Fail(
                    response.FailureReason,
                    response.ErrorMessage
                );
            }

            try
            {
                List<ClaimItemDto> claimItems = ParseClaimItems(response.Data);
                return PaymentResult<List<ClaimItemDto>>.Ok(claimItems);
            }
            catch (Exception exception)
            {
                return PaymentResult<List<ClaimItemDto>>.Fail(
                    PaymentFailureReason.ServerError,
                    $"Failed to parse claim response. {exception.Message}"
                );
            }
        }

        private bool EnsureValid<T>(out PaymentResult<T> result)
        {
            if (_isValid)
            {
                result = null;
                return true;
            }

            result = PaymentResult<T>.Fail(
                PaymentFailureReason.InvalidConfiguration,
                _configError
            );

            return false;
        }

        private void ApplyCommonHeaders(UnityWebRequest request)
        {
            request.timeout = Math.Max(1, _configuration.RequestTimeoutSeconds);

            request.SetRequestHeader(HeaderAccept, JsonContentType);
            request.SetRequestHeader(HeaderApiKey, _configuration.ApiKey);
        }

        private UnityWebRequest CreatePostJsonRequest(string url, string json)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Math.Max(1, _configuration.RequestTimeoutSeconds)
            };

            request.SetRequestHeader(HeaderContentType, JsonContentType);
            request.SetRequestHeader(HeaderAccept, JsonContentType);

            return request;
        }

        private async Task<PaymentResult<string>> SendAsync(UnityWebRequest request)
        {
            PaymentLogger.Log($"{request.method} {request.url}");

            UnityWebRequestAsyncOperation operation;

            try
            {
                operation = request.SendWebRequest();
            }
            catch (Exception exception)
            {
                return PaymentResult<string>.Fail(
                    PaymentFailureReason.NetworkError,
                    exception.Message
                );
            }

            TaskCompletionSource<bool> completion = new();

            operation.completed += _ =>
            {
                completion.TrySetResult(true);
            };

            await completion.Task;

            long statusCode = request.responseCode;
            string responseText = request.downloadHandler?.text;

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return PaymentResult<string>.Fail(
                    PaymentFailureReason.NetworkError,
                    BuildErrorMessage(request, responseText)
                );
            }

            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                return PaymentResult<string>.Fail(
                    MapHttpFailureReason(statusCode),
                    BuildErrorMessage(request, responseText)
                );
            }

            if (statusCode < 200 || statusCode >= 300)
            {
                return PaymentResult<string>.Fail(
                    MapHttpFailureReason(statusCode),
                    BuildErrorMessage(request, responseText)
                );
            }

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return PaymentResult<string>.Fail(
                    PaymentFailureReason.ServerError,
                    "Server returned an empty response."
                );
            }

            return PaymentResult<string>.Ok(responseText);
        }

        private PaymentFailureReason MapHttpFailureReason(long statusCode)
        {
            return statusCode switch
            {
                400 => PaymentFailureReason.BadRequest,
                401 => PaymentFailureReason.Unauthorized,
                403 => PaymentFailureReason.Forbidden,
                404 => PaymentFailureReason.ProductNotFound,
                409 => PaymentFailureReason.AlreadyClaimed,
                >= 500 and < 600 => PaymentFailureReason.ServerError,
                _ => PaymentFailureReason.Unknown
            };
        }

        private string BuildErrorMessage(UnityWebRequest request, string responseText)
        {
            string error = request.error;
            long statusCode = request.responseCode;

            if (string.IsNullOrWhiteSpace(responseText))
                return $"HTTP {statusCode}. {error}";

            return $"HTTP {statusCode}. {error}. Body: {responseText}";
        }

        private List<ClaimItemDto> ParseClaimItems(string json)
        {
            List<ClaimItemDto> result = new();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            string trimmed = json.Trim();

            if (trimmed.StartsWith("["))
            {
                string wrappedJson = "{\"items\":" + trimmed + "}";
                ClaimItemsWrapperDto wrapper =
                    JsonUtility.FromJson<ClaimItemsWrapperDto>(wrappedJson);

                if (wrapper?.items != null)
                    result.AddRange(wrapper.items);

                return result;
            }

            ClaimItemsWrapperDto wrapped =
                JsonUtility.FromJson<ClaimItemsWrapperDto>(trimmed);

            if (wrapped?.items != null)
                result.AddRange(wrapped.items);

            return result;
        }
    }
}