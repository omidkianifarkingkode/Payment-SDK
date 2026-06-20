using System;
using System.Collections.Generic;
using GamePaymentSDK.Core;
using UnityEngine;

namespace GamePaymentSDK.Storage
{
    public sealed class PlayerPrefsProcessedTransactionStorage : IProcessedTransactionStorage
    {
        private const string KeyPrefix = "GamePaymentSDK.ProcessedTransactions";

        private readonly string _storageKey;
        private readonly ILogger _logger;

        public PlayerPrefsProcessedTransactionStorage(string playerId, ILogger logger)
        {
            string safePlayerId = SanitizeKeyPart(playerId);

            _storageKey = $"{KeyPrefix}.{safePlayerId}";
            _logger = logger;
        }

        public bool IsProcessed(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return false;

            ProcessedTransactionCollection collection = LoadCollection();

            return collection.transactions.Exists(x => x.TransactionId == transactionId);
        }

        public void MarkProcessed(string transactionId, string orderId, string productKey)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                _logger.Log(LogType.Error, "[PaymentSdk] [PlayerPrefsProcessedTransactionStorage] Cannot mark processed transaction without transactionId.");
                return;
            }

            ProcessedTransactionCollection collection = LoadCollection();

            int existingIndex = collection.transactions.FindIndex(
                x => x.TransactionId == transactionId
            );

            ProcessedTransaction transaction = new()
            {
                TransactionId = transactionId,
                OrderId = orderId,
                ProductKey = productKey,
                ProcessedAtUnixSeconds = UnixTime.NowSeconds()
            };

            if (existingIndex >= 0)
            {
                collection.transactions[existingIndex] = transaction;
            }
            else
            {
                collection.transactions.Add(transaction);
            }

            SaveCollection(collection);

            _logger.Log(LogType.Log, $"[PaymentSdk] [PlayerPrefsProcessedTransactionStorage] Transaction marked as processed. transactionId={transactionId}, productKey={productKey}");
        }

        public List<ProcessedTransaction> GetAll()
        {
            ProcessedTransactionCollection collection = LoadCollection();
            return new List<ProcessedTransaction>(collection.transactions);
        }

        public void Remove(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return;

            ProcessedTransactionCollection collection = LoadCollection();

            int removedCount = collection.transactions.RemoveAll(
                x => x.TransactionId == transactionId
            );

            if (removedCount > 0)
            {
                SaveCollection(collection);

                _logger.Log(LogType.Log, $"[PaymentSdk] [PlayerPrefsProcessedTransactionStorage] Processed transaction removed. transactionId={transactionId}");
            }
        }

        public int RemoveOlderThan(long unixSeconds)
        {
            ProcessedTransactionCollection collection = LoadCollection();

            int removedCount = collection.transactions.RemoveAll(transaction =>
                transaction == null ||
                transaction.ProcessedAtUnixSeconds <= 0 ||
                transaction.ProcessedAtUnixSeconds < unixSeconds
            );

            if (removedCount > 0)
            {
                SaveCollection(collection);

                _logger.Log(LogType.Log, $"[PaymentSdk] [PlayerPrefsProcessedTransactionStorage] Old processed transactions removed. count={removedCount}, olderThan={unixSeconds}");
            }

            return removedCount;
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_storageKey);
            PlayerPrefs.Save();

            _logger.Log(LogType.Log, "[PaymentSdk] [PlayerPrefsProcessedTransactionStorage] Processed transaction storage cleared.");
        }

        private ProcessedTransactionCollection LoadCollection()
        {
            if (!PlayerPrefs.HasKey(_storageKey))
                return new ProcessedTransactionCollection();

            string json = PlayerPrefs.GetString(_storageKey);

            if (string.IsNullOrWhiteSpace(json))
                return new ProcessedTransactionCollection();

            try
            {
                ProcessedTransactionCollection collection =
                    JsonUtility.FromJson<ProcessedTransactionCollection>(json);

                if (collection == null)
                    return new ProcessedTransactionCollection();

                if (collection.transactions == null)
                    collection.transactions = new List<ProcessedTransaction>();

                RemoveInvalidTransactions(collection);

                return collection;
            }
            catch (Exception exception)
            {
                _logger.Log(LogType.Error,
                    $"Failed to parse processed transaction storage. Storage will be reset. {exception.Message}"
                );

                PlayerPrefs.DeleteKey(_storageKey);
                PlayerPrefs.Save();

                return new ProcessedTransactionCollection();
            }
        }

        private void SaveCollection(ProcessedTransactionCollection collection)
        {
            if (collection == null)
                collection = new ProcessedTransactionCollection();

            if (collection.transactions == null)
                collection.transactions = new List<ProcessedTransaction>();

            string json = JsonUtility.ToJson(collection);

            PlayerPrefs.SetString(_storageKey, json);
            PlayerPrefs.Save();
        }

        private void RemoveInvalidTransactions(ProcessedTransactionCollection collection)
        {
            collection.transactions.RemoveAll(transaction =>
                transaction == null ||
                string.IsNullOrWhiteSpace(transaction.TransactionId)
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
        private sealed class ProcessedTransactionCollection
        {
            public List<ProcessedTransaction> transactions = new();
        }
    }
}