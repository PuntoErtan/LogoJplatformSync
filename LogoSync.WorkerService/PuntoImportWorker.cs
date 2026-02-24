// =============================================
// LogoSync.WorkerService/PuntoImportWorker.cs
// =============================================

using LogoSync.Core.DTOs;
using LogoSync.Core.Helpers;
using LogoSync.Core.Interfaces;

namespace LogoSync.WorkerService
{
    /// <summary>
    /// PUNTO tablosundan verileri çekip JGDB05 SRC tablolarına aktaran worker
    /// - ERYAZ_TAHSILAT → SRC_CashReceipts (Nakit + Çek)
    /// - ERYAZ_SIPARISLER + ERYAZ_SIPARIS_DETAY → SRC_Orders + SRC_OrderDetails (Sipariş)
    /// </summary>
    public class PuntoImportWorker : BackgroundService
    {
        private readonly ILogger<PuntoImportWorker> _logger;
        private readonly IPuntoRepository _puntoRepository;
        private readonly ISyncRepository _syncRepository;
        private readonly int _importIntervalSeconds;
        private readonly int _batchSize;
        private readonly string _defaultCashAccountCode;
        private readonly string _jgdbConnectionString;

        // Modül açık/kapalı bayrakları
        private readonly bool _importReceiptEnabled;
        private readonly bool _importOrderEnabled;

        // Temsilci kod dönüşümü
        private readonly SalespersonHelper _salespersonHelper;

        public PuntoImportWorker(
            ILogger<PuntoImportWorker> logger,
            IPuntoRepository puntoRepository,
            ISyncRepository syncRepository,
            IConfiguration configuration)
        {
            _logger = logger;
            _puntoRepository = puntoRepository;
            _syncRepository = syncRepository;
            _importIntervalSeconds = configuration.GetValue("ImportSettings:IntervalSeconds", 30);
            _batchSize = configuration.GetValue("ImportSettings:BatchSize", 100);
            _defaultCashAccountCode = configuration.GetValue("ImportSettings:DefaultCashAccountCode", "01");
            _jgdbConnectionString = configuration.GetConnectionString("SqlConnection");

            // Modül ayarlarını oku (varsayılan: true)
            _importReceiptEnabled = configuration.GetValue("SyncModules:PuntoImportReceipt", true);
            _importOrderEnabled = configuration.GetValue("SyncModules:PuntoImportOrder", true);

            // Temsilci mapping'i oku
            var spMapping = configuration.GetSection("SalespersonMapping").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            _salespersonHelper = new SalespersonHelper(spMapping);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("PUNTO Import Worker Started");
            _logger.LogInformation("Interval: {Interval}s | Batch: {Batch} | CashAccount: {CashAccount}",
                _importIntervalSeconds, _batchSize, _defaultCashAccountCode);
            _logger.LogInformation("Modules => PuntoImportReceipt: {Receipt} | PuntoImportOrder: {Order}",
                _importReceiptEnabled ? "ON" : "OFF",
                _importOrderEnabled ? "ON" : "OFF");
            _logger.LogInformation("========================================");

            // İlk çalışmada biraz bekle (Logo sync worker'ın başlaması için)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tahsilat import (Nakit + Çek)
                    if (_importReceiptEnabled)
                        await ImportPendingReceiptsAsync(stoppingToken);

                    // Sipariş import
                    if (_importOrderEnabled)
                        await ImportPendingOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PUNTO import cycle");
                }

                await Task.Delay(TimeSpan.FromSeconds(_importIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("PUNTO Import Worker Stopped");
        }

        #region Tahsilat Import (Nakit + Çek)

        private async Task ImportPendingReceiptsAsync(CancellationToken stoppingToken)
        {
            // PUNTO'dan bekleyen kayıtları al
            var pendingReceipts = await _puntoRepository.GetPendingCashReceiptsAsync(_batchSize);
            var receiptList = pendingReceipts.ToList();

            if (!receiptList.Any())
            {
                _logger.LogDebug("No pending PUNTO receipt records");
                return;
            }

            _logger.LogInformation("Importing {Count} receipts from PUNTO...", receiptList.Count);

            var batchGuid = Guid.NewGuid().ToString();
            int successCount = 0, failCount = 0;

            foreach (var puntoReceipt in receiptList)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    // PUNTO verisini CashReceiptDto'ya dönüştür
                    var cashReceipt = MapToCashReceipt(puntoReceipt);

                    // SRC_CashReceipts'e ekle (Trigger otomatik olarak JPL_SyncQueue'ya da ekler)
                    var insertedId = await _syncRepository.InsertCashReceiptAsync(cashReceipt);

                    // PUNTO'daki kaydı aktarıldı olarak işaretle
                    await _puntoRepository.MarkAsTransferredAsync(puntoReceipt.Id, batchGuid);

                    _logger.LogDebug("Imported PUNTO receipt {PuntoId} → SRC_CashReceipts {NewId}",
                        puntoReceipt.Id, insertedId);

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import PUNTO receipt {PuntoId}", puntoReceipt.Id);
                    failCount++;
                }
            }

            _logger.LogInformation("PUNTO receipt import complete. Success: {Success}, Failed: {Failed}, BatchGuid: {BatchGuid}",
                successCount, failCount, batchGuid);

            await _syncRepository.LogAsync(null, "INFO",
                $"PUNTO receipt import: {successCount} success, {failCount} failed. BatchGuid: {batchGuid}");
        }

        /// <summary>
        /// PUNTO verisi → CashReceiptDto dönüşümü
        /// TAHSILAT_TIPI=0 → Nakit (N. prefix)
        /// TAHSILAT_TIPI=1 → Çek (C. prefix)
        /// </summary>
        private CashReceiptDto MapToCashReceipt(PuntoCashReceiptDto punto)
        {
            var receiptType = punto.TahsilatTipi ?? 0;

            // ReceiptNo prefix: N.5 (Nakit) veya C.6 (Çek)
            var prefix = receiptType == 1 ? "C" : "N";
            var receiptNo = $"{prefix}.{punto.No}";

            return new CashReceiptDto
            {
                ReceiptNo = receiptNo,
                ReceiptDate = punto.Tarih ?? DateTime.Today,
                CustomerCode = punto.CariKodu,
                CustomerName = punto.CariUnvani,
                CashAccountCode = _defaultCashAccountCode,
                Amount = punto.Tutar,
                CurrencyCode = "TRY",
                ExchangeRate = 1,
                Description = punto.Not ?? "",
                // Çek alanları
                ReceiptType = receiptType,
                BankName = punto.BankaAdi,
                ChequeNo = punto.CekSenetNo,
                DueDate = punto.VadeTarihi
            };
        }

        #endregion

        #region Sipariş Import

        /// <summary>
        /// PUNTO'dan siparişleri import eder
        /// ERYAZ_SIPARISLER (header) + ERYAZ_SIPARIS_DETAY (lines) → SRC_Orders + SRC_OrderDetails
        /// </summary>
        private async Task ImportPendingOrdersAsync(CancellationToken stoppingToken)
        {
            var pendingOrders = await _puntoRepository.GetPendingOrdersAsync(_batchSize);

            if (pendingOrders == null || pendingOrders.Count == 0)
            {
                _logger.LogDebug("No pending PUNTO order records");
                return;
            }

            _logger.LogInformation("Importing {Count} orders from PUNTO...", pendingOrders.Count);

            var batchGuid = Guid.NewGuid().ToString();
            int successCount = 0, failCount = 0;

            foreach (var puntoOrder in pendingOrders)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    // 1. Detay satırlarını getir
                    var details = await _puntoRepository.GetOrderDetailsAsync(puntoOrder.FisNo);

                    if (details == null || details.Count == 0)
                    {
                        _logger.LogWarning("PUNTO Order FisNo={FisNo} has no detail lines, skipping.", puntoOrder.FisNo);
                        failCount++;
                        continue;
                    }

                    // 2. DEPO'dan Warehouse ve OrgUnit türet: "7" → "01.7.7" / "01.7"
                    var warehouse = DeriveWarehouse(puntoOrder.Depo);
                    var orgUnit = DeriveOrgUnit(puntoOrder.Depo);

                    // 2.5. Temsilci kodunu dönüştür
                    puntoOrder.TemsilciKodu = _salespersonHelper.Resolve(puntoOrder.TemsilciKodu);

                    // 3. SRC_Orders'a header ekle
                    var orderId = await InsertOrderHeaderAsync(puntoOrder, warehouse, orgUnit);

                    // 4. SRC_OrderDetails'a detay satırları ekle
                    await InsertOrderDetailsAsync(orderId, details);

                    // 5. PUNTO'da aktarıldı olarak işaretle
                    await _puntoRepository.MarkOrderAsTransferredAsync(puntoOrder.Id, batchGuid);

                    _logger.LogDebug("Imported PUNTO order {PuntoId} FisNo={FisNo} → SRC_Orders {OrderId} ({LineCount} lines)",
                        puntoOrder.Id, puntoOrder.FisNo, orderId, details.Count);

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import PUNTO order {PuntoId} FisNo={FisNo}",
                        puntoOrder.Id, puntoOrder.FisNo);
                    failCount++;
                }
            }

            _logger.LogInformation("PUNTO order import complete. Success: {Success}, Failed: {Failed}, BatchGuid: {BatchGuid}",
                successCount, failCount, batchGuid);

            await _syncRepository.LogAsync(null, "INFO",
                $"PUNTO order import: {successCount} success, {failCount} failed. BatchGuid: {batchGuid}");
        }

        /// <summary>
        /// SRC_Orders'a sipariş başlık kaydı ekler
        /// Trigger otomatik olarak JPL_SyncQueue'ya EntityType='Order' kaydı ekler
        /// </summary>
        private async Task<long> InsertOrderHeaderAsync(PuntoOrderDto puntoOrder, string warehouse, string orgUnit)
        {
            const string sql = @"
                INSERT INTO [dbo].[SRC_Orders]
                (
                    [FisNo], [OrderNo], [OrderDate], [CustomerCode], [CustomerName],
                    [DocumentNo], [ShipmentMethod], [SalespersonCode],
                    [Warehouse], [OrgUnit], [OrderNote], [SpecialCode],
                    [IsSynced], [CreatedAt], [PuntoId]
                )
                VALUES
                (
                    @FisNo, @OrderNo, @OrderDate, @CustomerCode, @CustomerName,
                    @DocumentNo, @ShipmentMethod, @SalespersonCode,
                    @Warehouse, @OrgUnit, @OrderNote, @SpecialCode,
                    0, GETDATE(), @PuntoId
                );
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_jgdbConnectionString);
            using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FisNo", (object)puntoOrder.FisNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrderNo", (object)puntoOrder.No ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrderDate", puntoOrder.SiparisTarihi.Date);
            command.Parameters.AddWithValue("@CustomerCode", puntoOrder.CariKodu);
            command.Parameters.AddWithValue("@CustomerName", (object)puntoOrder.CariUnvani ?? DBNull.Value);
            command.Parameters.AddWithValue("@DocumentNo", (object)puntoOrder.BelgeNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipmentMethod", (object)puntoOrder.GonderiSekli ?? DBNull.Value);
            command.Parameters.AddWithValue("@SalespersonCode", (object)puntoOrder.TemsilciKodu ?? DBNull.Value);
            command.Parameters.AddWithValue("@Warehouse", (object)warehouse ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrgUnit", (object)orgUnit ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrderNote", (object)puntoOrder.SiparisNotu ?? DBNull.Value);
            command.Parameters.AddWithValue("@SpecialCode", (object)puntoOrder.OzelKod ?? DBNull.Value);
            command.Parameters.AddWithValue("@PuntoId", puntoOrder.Id);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return (long)result;
        }

        /// <summary>
        /// SRC_OrderDetails'a sipariş detay satırları ekler
        /// ISK_KAMPANYA, ISK_CEP, ISK3, ISK4 iskonto alanları dahil
        /// </summary>
        private async Task InsertOrderDetailsAsync(long orderId, List<PuntoOrderDetailDto> details)
        {
            const string sql = @"
                INSERT INTO [dbo].[SRC_OrderDetails]
                (
                    [OrderId], [ProductCode], [ProductName],
                    [Quantity], [UnitPrice], [UnitCode],
                    [DiscountCampaign], [DiscountMobile],
                    [DiscountIsk3], [DiscountIsk4],
                    [LineOrder]
                )
                VALUES
                (
                    @OrderId, @ProductCode, @ProductName,
                    @Quantity, @UnitPrice, @UnitCode,
                    @DiscountCampaign, @DiscountMobile,
                    @DiscountIsk3, @DiscountIsk4,
                    @LineOrder
                )";

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_jgdbConnectionString);
            await connection.OpenAsync();

            int lineOrder = 1;
            foreach (var detail in details)
            {
                using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@OrderId", orderId);
                command.Parameters.AddWithValue("@ProductCode", (object)detail.UrunKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@ProductName", (object)detail.UrunAdi ?? DBNull.Value);
                command.Parameters.AddWithValue("@Quantity", detail.Miktar);
                command.Parameters.AddWithValue("@UnitPrice", detail.BirimFiyat);
                command.Parameters.AddWithValue("@UnitCode", (object)detail.Birim ?? "ADET");
                command.Parameters.AddWithValue("@DiscountCampaign", (object)detail.IskKampanya ?? 0);
                command.Parameters.AddWithValue("@DiscountMobile", (object)detail.IskCep ?? 0);
                command.Parameters.AddWithValue("@DiscountIsk3", (object)detail.Isk3 ?? 0);
                command.Parameters.AddWithValue("@DiscountIsk4", (object)detail.Isk4 ?? 0);
                command.Parameters.AddWithValue("@LineOrder", lineOrder++);

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// DEPO alanından Warehouse türetir: "7" → "01.7.7", "42" → "01.42.42"
        /// </summary>
        private static string DeriveWarehouse(string depo)
        {
            if (string.IsNullOrEmpty(depo))
                return null;

            var d = depo.Trim();
            return $"01.{d}.{d}";
        }

        /// <summary>
        /// DEPO alanından OrgUnit türetir: "7" → "01.7", "42" → "01.42"
        /// </summary>
        private static string DeriveOrgUnit(string depo)
        {
            if (string.IsNullOrEmpty(depo))
                return null;

            var d = depo.Trim();
            return $"01.{d}";
        }

        #endregion
    }
}
