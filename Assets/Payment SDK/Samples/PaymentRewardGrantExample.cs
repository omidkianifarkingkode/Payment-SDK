using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePaymentSDK.Samples
{
    public sealed class PaymentRewardGrantExample : MonoBehaviour
    {
        [Serializable]
        public sealed class ProductReward
        {
            public string ProductKey;
            public int Gems;
            public int Coins;
        }

        [SerializeField] private List<ProductReward> _rewards = new();

        public void Grant(string productKey)
        {
            ProductReward reward = _rewards.Find(x => x.ProductKey == productKey);

            if (reward == null)
            {
                Debug.LogWarning($"[PaymentRewardGrantExample] No reward configured for productKey={productKey}");
                return;
            }

            if (reward.Gems > 0)
            {
                Debug.Log($"[PaymentRewardGrantExample] Grant gems: {reward.Gems}");
                // CurrencyManager.AddGems(reward.Gems);
            }

            if (reward.Coins > 0)
            {
                Debug.Log($"[PaymentRewardGrantExample] Grant coins: {reward.Coins}");
                // CurrencyManager.AddCoins(reward.Coins);
            }

            Debug.Log($"[PaymentRewardGrantExample] Reward granted for productKey={productKey}");
        }
    }
}