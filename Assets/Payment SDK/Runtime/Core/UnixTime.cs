using System;

namespace GamePaymentSDK.Core
{
    public static class UnixTime
    {
        public static long NowSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}