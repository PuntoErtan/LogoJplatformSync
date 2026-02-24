// =============================================
// LogoSync.Infrastructure/Data/SyncRepository.cs
// =============================================

using System.Data;
using Microsoft.Data.SqlClient;
using LogoSync.Core.DTOs;
using LogoSync.Core.Entities;
using LogoSync.Core.Interfaces;

namespace LogoSync.Infrastructure.Data
{
    public class SyncRepository : ISyncRepository
    {
        private readonly string _connectionString;

        public SyncRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Sync Queue & Log Methods

        public async Task<IEnumerable<SyncQueueItem>> GetPendingRecordsAsync(string entityType, int batchSize)
        {
            var items = new List<SyncQueueItem>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("JPL_GetPendingRecords", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@EntityType", entityType);
            command.Parameters.AddWithValue("@BatchSize", batchSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new SyncQueueItem
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    EntityType = reader.GetString(reader.GetOrdinal("EntityType")),
                    EntityId = reader.GetString(reader.GetOrdinal("EntityId")),
                    OperationType = reader.GetString(reader.GetOrdinal("OperationType")),
                    Payload = reader.IsDBNull(reader.GetOrdinal("Payload"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Payload")),
                    RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount"))
                });
            }

            return items;
        }

        public async Task UpdateSyncStatusAsync(long id, SyncStatus status, string errorMessage = null, string jplatformRef = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("JPL_UpdateSyncStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Status", (int)status);
            command.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@JplatformRef", (object)jplatformRef ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task LogAsync(long? syncQueueId, string logLevel, string message,
            string requestData = null, string responseData = null)
        {
            const string sql = @"
                INSERT INTO [dbo].[JPL_SyncLog] 
                    ([SyncQueueId], [LogLevel], [Message], [RequestData], [ResponseData])
                VALUES 
                    (@SyncQueueId, @LogLevel, @Message, @RequestData, @ResponseData)";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@SyncQueueId", (object)syncQueueId ?? DBNull.Value);
            command.Parameters.AddWithValue("@LogLevel", logLevel);
            command.Parameters.AddWithValue("@Message", message);
            command.Parameters.AddWithValue("@RequestData", (object)requestData ?? DBNull.Value);
            command.Parameters.AddWithValue("@ResponseData", (object)responseData ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<string> GetConfigValueAsync(string key)
        {
            const string sql = "SELECT [Value] FROM [dbo].[JPL_Config] WHERE [Key] = @Key";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Key", key);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();

            return result?.ToString();
        }

        #endregion

        #region CashReceipt Methods

        /// <summary>
        /// PUNTO'dan gelen veriyi SRC_CashReceipts'e ekler
        /// Trigger otomatik olarak JPL_SyncQueue'ya da ekler
        /// </summary>
        public async Task<long> InsertCashReceiptAsync(CashReceiptDto receipt)
        {
            const string sql = @"
                INSERT INTO [dbo].[SRC_CashReceipts]
                    ([ReceiptNo], [ReceiptDate], [CustomerCode], [CustomerName], 
                     [CashAccountCode], [Amount], [CurrencyCode], [ExchangeRate], [Description],
                     [ReceiptType], [BankName], [ChequeNo], [DueDate])
                VALUES
                    (@ReceiptNo, @ReceiptDate, @CustomerCode, @CustomerName,
                     @CashAccountCode, @Amount, @CurrencyCode, @ExchangeRate, @Description,
                     @ReceiptType, @BankName, @ChequeNo, @DueDate);
                
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ReceiptNo", receipt.ReceiptNo);
            command.Parameters.AddWithValue("@ReceiptDate", receipt.ReceiptDate);
            command.Parameters.AddWithValue("@CustomerCode", receipt.CustomerCode);
            command.Parameters.AddWithValue("@CustomerName", (object)receipt.CustomerName ?? DBNull.Value);
            command.Parameters.AddWithValue("@CashAccountCode", receipt.CashAccountCode);
            command.Parameters.AddWithValue("@Amount", receipt.Amount);
            command.Parameters.AddWithValue("@CurrencyCode", receipt.CurrencyCode ?? "TRY");
            command.Parameters.AddWithValue("@ExchangeRate", receipt.ExchangeRate > 0 ? receipt.ExchangeRate : 1);
            command.Parameters.AddWithValue("@Description", (object)receipt.Description ?? DBNull.Value);
            // Çek alanları
            command.Parameters.AddWithValue("@ReceiptType", receipt.ReceiptType);
            command.Parameters.AddWithValue("@BankName", (object)receipt.BankName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ChequeNo", (object)receipt.ChequeNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@DueDate", (object)receipt.DueDate ?? DBNull.Value);

            await connection.OpenAsync();
            var insertedId = await command.ExecuteScalarAsync();

            return Convert.ToInt64(insertedId);
        }

        #endregion

        #region Order Methods

        /// <summary>
        /// Sipariş başlık + detay verilerini getirir
        /// JPL_GetOrderWithDetails SP'sini çağırır
        /// </summary>
        public async Task<OrderDto> GetOrderWithDetailsAsync(long orderId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("JPL_GetOrderWithDetails", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await command.ExecuteReaderAsync();

            OrderDto order = null;

            // 1. Header
            if (await reader.ReadAsync())
            {
                order = new OrderDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    FisNo = reader.GetString(reader.GetOrdinal("FisNo")),
                    OrderNo = reader.IsDBNull(reader.GetOrdinal("OrderNo")) ? null : reader.GetString(reader.GetOrdinal("OrderNo")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    CustomerCode = reader.GetString(reader.GetOrdinal("CustomerCode")),
                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName")),
                    DocumentNo = reader.IsDBNull(reader.GetOrdinal("DocumentNo")) ? null : reader.GetString(reader.GetOrdinal("DocumentNo")),
                    ShipmentMethod = reader.IsDBNull(reader.GetOrdinal("ShipmentMethod")) ? null : reader.GetString(reader.GetOrdinal("ShipmentMethod")),
                    SalespersonCode = reader.IsDBNull(reader.GetOrdinal("SalespersonCode")) ? null : reader.GetString(reader.GetOrdinal("SalespersonCode")),
                    Warehouse = reader.IsDBNull(reader.GetOrdinal("Warehouse")) ? null : reader.GetString(reader.GetOrdinal("Warehouse")),
                    OrgUnit = reader.IsDBNull(reader.GetOrdinal("OrgUnit")) ? null : reader.GetString(reader.GetOrdinal("OrgUnit")),
                    OrderNote = reader.IsDBNull(reader.GetOrdinal("OrderNote")) ? null : reader.GetString(reader.GetOrdinal("OrderNote")),
                    SpecialCode = reader.IsDBNull(reader.GetOrdinal("SpecialCode")) ? null : reader.GetString(reader.GetOrdinal("SpecialCode")),
                    IsSynced = reader.GetBoolean(reader.GetOrdinal("IsSynced")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    PuntoId = reader.IsDBNull(reader.GetOrdinal("PuntoId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PuntoId"))
                };
            }

            if (order == null)
                return null;

            // 2. Details
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    order.Details.Add(new OrderDetailDto
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),
                        OrderId = reader.GetInt64(reader.GetOrdinal("OrderId")),
                        ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
                        ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader.GetString(reader.GetOrdinal("ProductName")),
                        Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                        UnitCode = reader.IsDBNull(reader.GetOrdinal("UnitCode")) ? "ADET" : reader.GetString(reader.GetOrdinal("UnitCode")),
                        DiscountCampaign = reader.IsDBNull(reader.GetOrdinal("DiscountCampaign")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("DiscountCampaign")),
                        DiscountMobile = reader.IsDBNull(reader.GetOrdinal("DiscountMobile")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("DiscountMobile")),
                        DiscountIsk3 = reader.IsDBNull(reader.GetOrdinal("DiscountIsk3")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("DiscountIsk3")),
                        DiscountIsk4 = reader.IsDBNull(reader.GetOrdinal("DiscountIsk4")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("DiscountIsk4")),
                        LineOrder = reader.GetInt32(reader.GetOrdinal("LineOrder"))
                    });
                }
            }

            return order;
        }

        /// <summary>
        /// Cari kodundan ödeme planı kodunu getirir
        /// Logo U_005_PAYPLANS + U_005_ARPS tabloları üzerinden
        /// </summary>
        public async Task<string> GetPaymentPlanByCustomerAsync(string customerCode)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("JPL_GetPaymentPlanByCustomer", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@CustomerCode", customerCode);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? result.ToString() : null;
        }

        #endregion
    }
}
