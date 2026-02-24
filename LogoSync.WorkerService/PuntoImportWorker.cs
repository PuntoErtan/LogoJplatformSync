// =============================================
// LogoSync.WorkerService/PuntoImportWorker.cs
// =============================================

using LogoSync.Core.DTOs;
using LogoSync.Core.Interfaces;

namespace LogoSync.WorkerService
{
    /// <summary>
    /// PUNTO tablosundan verileri çekip JGDB05 SRC tablolarına aktaran worker
    /// - ERYAZ_TAHSILAT → SRC_CashReceipts (Nakit + Çek)
    /// Not: Sipariş import artık Worker.ProcessPuntoOrdersAsync() tarafından doğrudan yapılmaktadır.
    /// </summary>
    public class PuntoImportWorker : BackgroundService
    {
        private readonly ILogger<PuntoImportWorker> _logger;
        private readonly IPuntoRepository _puntoRepository;
        private readonly ISyncRepository _syncRepository;
        private readonly int _importIntervalSeconds;
        private readonly int _batchSize;
        private readonly string _defaultCashAccountCode;

        // Modül açık/kapalı bayrakları
        private readonly bool _importReceiptEnabled;

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

            // Modül ayarlarını oku (varsayılan: true)
            _importReceiptEnabled = configuration.GetValue("SyncModules:PuntoImportReceipt", true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("PUNTO Import Worker Started");
            _logger.LogInformation("Interval: {Interval}s | Batch: {Batch} | CashAccount: {CashAccount}",
                _importIntervalSeconds, _batchSize, _defaultCashAccountCode);
            _logger.LogInformation("Modules => PuntoImportReceipt: {Receipt}",
                _importReceiptEnabled ? "ON" : "OFF");
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
    }
}
