/// Storage Is Scoped by Client and Player
/// The final key becomes something like: GamePaymentSDK.PendingOrders.client_123.player123
/// This prevents this bad situation:
/// * Player A has pending order
/// * Player logs out
/// * Player B logs in
/// * SDK accidentally claims Player A's pending order for Player B

using System;
using System.Collections.Generic;
using GamePaymentSDK.Core;
using UnityEngine;

namespace GamePaymentSDK.Storage
{
    public sealed class PlayerPrefsPendingOrderStorage : IPendingOrderStorage
    {
        private const string KeyPrefix = "GamePaymentSDK.PendingOrders";

        private readonly string _storageKey;

        public PlayerPrefsPendingOrderStorage(string clientId, string playerId)
        {
            string safeClientId = SanitizeKeyPart(clientId);
            string safePlayerId = SanitizeKeyPart(playerId);

            _storageKey = $"{KeyPrefix}.{safeClientId}.{safePlayerId}";
        }

        public void Save(PendingOrder order)
        {
            if (order == null)
            {
                PaymentLogger.LogWarning("Cannot save null pending order.");
                return;
            }

            if (string.IsNullOrWhiteSpace(order.OrderId))
            {
                PaymentLogger.LogWarning("Cannot save pending order without OrderId.");
                return;
            }

            PendingOrderCollection collection = LoadCollection();

            int existingIndex = collection.orders.FindIndex(x => x.OrderId == order.OrderId);

            if (existingIndex >= 0)
            {
                collection.orders[existingIndex] = order;
            }
            else
            {
                collection.orders.Add(order);
            }

            SaveCollection(collection);

            PaymentLogger.Log($"Pending order saved. orderId={order.OrderId}, status={order.Status}");
        }

        public bool TryGet(string orderId, out PendingOrder order)
        {
            order = null;

            if (string.IsNullOrWhiteSpace(orderId))
                return false;

            PendingOrderCollection collection = LoadCollection();

            order = collection.orders.Find(x => x.OrderId == orderId);

            return order != null;
        }

        public List<PendingOrder> GetAll()
        {
            PendingOrderCollection collection = LoadCollection();

            return new List<PendingOrder>(collection.orders);
        }

        public void Remove(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return;

            PendingOrderCollection collection = LoadCollection();

            int removedCount = collection.orders.RemoveAll(x => x.OrderId == orderId);

            if (removedCount > 0)
            {
                SaveCollection(collection);
                PaymentLogger.Log($"Pending order removed. orderId={orderId}");
            }
        }

        public int RemoveOlderThan(long unixSeconds)
        {
            PendingOrderCollection collection = LoadCollection();
        
            int removedCount = collection.orders.RemoveAll(order =>
                order == null ||
                order.CreatedAtUnixSeconds <= 0 ||
                order.CreatedAtUnixSeconds < unixSeconds
            );
        
            if (removedCount > 0)
            {
                SaveCollection(collection);
        
                PaymentLogger.Log(
                    $"Old pending orders removed. count={removedCount}, olderThan={unixSeconds}"
                );
            }
        
            return removedCount;
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_storageKey);
            PlayerPrefs.Save();

            PaymentLogger.Log("Pending order storage cleared.");
        }

        private PendingOrderCollection LoadCollection()
        {
            if (!PlayerPrefs.HasKey(_storageKey))
                return new PendingOrderCollection();

            string json = PlayerPrefs.GetString(_storageKey);

            if (string.IsNullOrWhiteSpace(json))
                return new PendingOrderCollection();

            try
            {
                PendingOrderCollection collection =
                    JsonUtility.FromJson<PendingOrderCollection>(json);

                if (collection == null)
                    return new PendingOrderCollection();

                if (collection.orders == null)
                    collection.orders = new List<PendingOrder>();

                RemoveInvalidOrders(collection);

                return collection;
            }
            catch (Exception exception)
            {
                PaymentLogger.LogWarning(
                    $"Failed to parse pending order storage. Storage will be reset. {exception.Message}"
                );

                PlayerPrefs.DeleteKey(_storageKey);
                PlayerPrefs.Save();

                return new PendingOrderCollection();
            }
        }

        private void SaveCollection(PendingOrderCollection collection)
        {
            if (collection == null)
                collection = new PendingOrderCollection();

            if (collection.orders == null)
                collection.orders = new List<PendingOrder>();

            string json = JsonUtility.ToJson(collection);

            PlayerPrefs.SetString(_storageKey, json);
            PlayerPrefs.Save();
        }

        private void RemoveInvalidOrders(PendingOrderCollection collection)
        {
            collection.orders.RemoveAll(order =>
                order == null ||
                string.IsNullOrWhiteSpace(order.OrderId) ||
                string.IsNullOrWhiteSpace(order.PlayerId) ||
                string.IsNullOrWhiteSpace(order.ProductKey)
            );
        }

        private static string SanitizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            return value
                .Trim()
                .Replace(" ", "_")
                .Replace(".", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_");
        }

        [Serializable]
        private sealed class PendingOrderCollection
        {
            public List<PendingOrder> orders = new();
        }
    }
}