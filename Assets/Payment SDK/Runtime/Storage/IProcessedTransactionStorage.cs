using System.Collections.Generic;
using GamePaymentSDK.Core;

namespace GamePaymentSDK.Storage
{
    public interface IProcessedTransactionStorage
    {
        bool IsProcessed(string transactionId);

        void MarkProcessed(
            string transactionId,
            string orderId,
            string productKey
        );

        List<ProcessedTransaction> GetAll();

        void Remove(string transactionId);
        
        int RemoveOlderThan(long unixSeconds);

        void Clear();
    }
}