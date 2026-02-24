// =============================================
// LogoSync.Core/Interfaces/ISyncRepository.cs
// =============================================

using LogoSync.Core.DTOs;
using LogoSync.Core.Entities;

namespace LogoSync.Core.Interfaces
{
    public interface ISyncRepository
    {
        // Sync Queue & Log
        Task<IEnumerable<SyncQueueItem>> GetPendingRecordsAsync(string entityType, int batchSize);
        Task UpdateSyncStatusAsync(long id, SyncStatus status, string errorMessage = null, string jplatformRef = null);
        Task LogAsync(long? syncQueueId, string logLevel, string message, string requestData = null, string responseData = null);
        Task<string> GetConfigValueAsync(string key);

        // CashReceipt
        Task<long> InsertCashReceiptAsync(CashReceiptDto receipt);

        // Order
        [Obsolete("SRC_Orders ara tablosu kaldırıldı. Worker artık doğrudan PUNTO'dan okuyor.")]
        Task<OrderDto> GetOrderWithDetailsAsync(long orderId);
        Task<string> GetPaymentPlanByCustomerAsync(string customerCode);
    }
}
