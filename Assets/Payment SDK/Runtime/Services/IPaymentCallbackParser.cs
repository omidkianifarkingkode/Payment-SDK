using GamePaymentSDK.Core;

namespace GamePaymentSDK.Services
{
    public interface IPaymentCallbackParser
    {
        PaymentCallbackResult Parse(string url);
    }
}