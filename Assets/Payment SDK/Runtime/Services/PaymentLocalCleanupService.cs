using System;
using GamePaymentSDK.Core;
using GamePaymentSDK.Storage;

namespace GamePaymentSDK.Services
{
    public sealed class PaymentLocalCleanupService
    {
        private readonly PaymentConfiguration _configuration;
        private readonly IPendingOrderStorage _pendingOrderStorage;
        private readonly IProcessedTransactionStorage _processedTransactionStorage;

        public PaymentLocalCleanupService(
            PaymentConfiguration configuration,
            IPendingOrderStorage pendingOrderStorage,
            IProcessedTransactionStorage processedTransactionStorage
        )
        {
            _configuration = configuration;
            _pendingOrderStorage = pendingOrderStorage;
            _processedTransactionStorage = processedTransactionStorage;
        }

        public void RunCleanup()
        {
            CleanupPendingOrders();
            CleanupProcessedTransactions();
        }

        private void CleanupPendingOrders()
        {
            if (_pendingOrderStorage == null)
                return;

            int days = _configuration?.PendingOrderCleanupDays ?? 7;

            if (days <= 0)
                return;

            long threshold = DateTimeOffset.UtcNow
                .AddDays(-days)
                .ToUnixTimeSeconds();

            int removedCount = _pendingOrderStorage.RemoveOlderThan(threshold);

            if (removedCount > 0)
            {
                PaymentLogger.Log(
                    $"Pending order cleanup completed. removedCount={removedCount}"
                );
            }
        }

        private void CleanupProcessedTransactions()
        {
            if (_processedTransactionStorage == null)
                return;

            int days = _configuration?.ProcessedTransactionHistoryDays ?? 90;

            if (days <= 0)
                return;

            long threshold = DateTimeOffset.UtcNow
                .AddDays(-days)
                .ToUnixTimeSeconds();

            int removedCount = _processedTransactionStorage.RemoveOlderThan(threshold);

            if (removedCount > 0)
            {
                PaymentLogger.Log(
                    $"Processed transaction cleanup completed. removedCount={removedCount}"
                );
            }
        }
    }
}