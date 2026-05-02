using System;
using System.Collections.Generic;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public sealed class PaymentCallbackParser : IPaymentCallbackParser
    {
        private readonly PaymentConfiguration _configuration;
        private readonly string _expectedCallbackPath;

        public PaymentCallbackParser(PaymentConfiguration configuration)
        {
            _configuration = configuration;

            string clientId = configuration?.ClientId ?? string.Empty;
            _expectedCallbackPath = $"/v1/payments/callback/{clientId}";
        }

        public PaymentCallbackResult Parse(string url)
        {
            PaymentCallbackResult result = new PaymentCallbackResult
            {
                RawUrl = url,
                IsPaymentCallback = false,
                IsSuccess = false,
                Status = PaymentCallbackStatus.Unknown
            };

            if (string.IsNullOrWhiteSpace(url))
                return result;

            if (_configuration == null || string.IsNullOrWhiteSpace(_configuration.ClientId))
                return result;

            Uri uri;

            try
            {
                uri = new Uri(url);
            }
            catch
            {
                return result;
            }

            if (!IsExpectedCallbackPath(uri))
                return result;

            Dictionary<string, string> query = ParseQuery(uri.Query);

            query.TryGetValue("authority", out string authority);
            query.TryGetValue("status", out string statusRaw);

            PaymentCallbackStatus status = ParseStatus(statusRaw);

            result.IsPaymentCallback = true;
            result.ClientId = _configuration.ClientId;
            result.Authority = authority;
            result.StatusRaw = statusRaw;
            result.Status = status;
            result.IsSuccess = status == PaymentCallbackStatus.Ok;

            return result;
        }

        private bool IsExpectedCallbackPath(Uri uri)
        {
            string path = uri.AbsolutePath;

            if (string.IsNullOrWhiteSpace(path))
                return false;

            return string.Equals(
                path.TrimEnd('/'),
                _expectedCallbackPath.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase
            );
        }

        private PaymentCallbackStatus ParseStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return PaymentCallbackStatus.Unknown;

            if (string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                return PaymentCallbackStatus.Ok;

            if (string.Equals(status, "NOK", StringComparison.OrdinalIgnoreCase))
                return PaymentCallbackStatus.Nok;

            return PaymentCallbackStatus.Unknown;
        }

        private Dictionary<string, string> ParseQuery(string queryString)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(queryString))
                return result;

            string query = queryString;

            if (query.StartsWith("?"))
                query = query.Substring(1);

            string[] pairs = query.Split('&');

            foreach (string pair in pairs)
            {
                if (string.IsNullOrWhiteSpace(pair))
                    continue;

                string[] parts = pair.Split(new[] { '=' }, 2);

                string key = UrlDecode(parts[0]);
                string value = parts.Length > 1 ? UrlDecode(parts[1]) : string.Empty;

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                result[key] = value;
            }

            return result;
        }

        private string UrlDecode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return Uri.UnescapeDataString(value.Replace("+", " "));
        }
    }
}