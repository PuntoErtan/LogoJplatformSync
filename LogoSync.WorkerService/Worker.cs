using System.Text.Json;
using System.Text.Encodings.Web;
using LogoSync.Core.DTOs;
using LogoSync.Core.Entities;
using LogoSync.Core.Helpers;
using LogoSync.Core.Interfaces;
using LogoSync.Core.Mappers;

namespace LogoSync.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ISyncRepository _repository;
        private readonly IJplatformApiClient _apiClient;
        private readonly IPuntoRepository _puntoRepository;
        private readonly IConfiguration _configuration;
        private readonly int _syncIntervalSeconds;
        private readonly int _batchSize;
        private readonly JsonSerializerOptions _jsonOptions;

        // Modül açık/kapalı bayrakları
        private readonly bool _cashReceiptEnabled;
        private readonly bool _chequeReceiptEnabled;
        private readonly bool _orderEnabled;
        private readonly bool _sanalPosEnabled;
        private readonly bool _salesInvoiceEnabled;

        // Temsilci kod dönüşümü
        private readonly SalespersonHelper _salespersonHelper;

        public Worker(
            ILogger<Worker> logger,
            ISyncRepository repository,
            IJplatformApiClient apiClient,
            IPuntoRepository puntoRepository,
            IConfiguration configuration)
        {
            _logger = logger;
            _repository = repository;
            _apiClient = apiClient;
            _puntoRepository = puntoRepository;
            _configuration = configuration;
            _syncIntervalSeconds = configuration.GetValue("SyncSettings:IntervalSeconds", 60);
            _batchSize = configuration.GetValue("SyncSettings:BatchSize", 50);

            // Modül ayarlarını oku (varsayılan: true)
            _cashReceiptEnabled = configuration.GetValue("SyncModules:CashReceipt", true);
            _chequeReceiptEnabled = configuration.GetValue("SyncModules:ChequeReceipt", true);
            _orderEnabled = configuration.GetValue("SyncModules:Order", true);
            _sanalPosEnabled = configuration.GetValue("SyncModules:SanalPos", true);
            _salesInvoiceEnabled = configuration.GetValue("SyncModules:SalesInvoice", true);

            // Temsilci mapping'i oku
            var spMapping = configuration.GetSection("SalespersonMapping").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            _salespersonHelper = new SalespersonHelper(spMapping);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("Logo jPlatform Sync Service Started");
            _logger.LogInformation("Interval: {Interval}s | Batch: {Batch}", _syncIntervalSeconds, _batchSize);
            _logger.LogInformation("Modules => CashReceipt: {Cash} | ChequeReceipt: {Cheque} | Order: {Order} | SanalPos: {SanalPos} | SalesInvoice: {SalesInvoice}",
                _cashReceiptEnabled ? "ON" : "OFF",
                _chequeReceiptEnabled ? "ON" : "OFF",
                _orderEnabled ? "ON" : "OFF",
                _sanalPosEnabled ? "ON" : "OFF",
                _salesInvoiceEnabled ? "ON" : "OFF");
            _logger.LogInformation("========================================");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Nakit Tahsilatları işle
                    if (_cashReceiptEnabled)
                        await ProcessPendingRecordsAsync("CashReceipt", stoppingToken);

                    // Çek Giriş Bordrolarını işle
                    if (_chequeReceiptEnabled)
                        await ProcessPendingRecordsAsync("ChequeReceipt", stoppingToken);

                    // Siparişleri işle
                    if (_orderEnabled)
                        await ProcessPendingRecordsAsync("Order", stoppingToken);

                    // Sanal POS → Cari Hesap Fişi (ArpSlip) işle
                    if (_sanalPosEnabled)
                        await ProcessSanalPosAsync(stoppingToken);

                    // İrsaliyeden Faturalama → POST /invoices/sales?invoiceType=8
                    if (_salesInvoiceEnabled)
                        await ProcessSalesInvoicesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in sync cycle");
                }

                await Task.Delay(TimeSpan.FromSeconds(_syncIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Service Stopped");
        }

        private async Task ProcessPendingRecordsAsync(string entityType, CancellationToken stoppingToken)
        {
            var pendingRecords = await _repository.GetPendingRecordsAsync(entityType, _batchSize);
            var recordList = pendingRecords.ToList();

            if (!recordList.Any())
            {
                _logger.LogDebug("No pending {EntityType} records", entityType);
                return;
            }

            _logger.LogInformation("Processing {Count} {EntityType} records...", recordList.Count, entityType);

            int successCount = 0, failCount = 0;

            foreach (var record in recordList)
            {
                if (stoppingToken.IsCancellationRequested) break;

                bool result;
                if (entityType == "ChequeReceipt")
                    result = await ProcessChequeRecordAsync(record);
                else if (entityType == "Order")
                    result = await ProcessOrderRecordAsync(record);
                else
                    result = await ProcessCashRecordAsync(record);

                if (result)
                    successCount++;
                else
                    failCount++;
            }

            _logger.LogInformation("{EntityType} batch complete. Success: {Success}, Failed: {Failed}",
                entityType, successCount, failCount);
        }

        /// <summary>
        /// Nakit Tahsilat işle (CashReceipt)
        /// </summary>
        private async Task<bool> ProcessCashRecordAsync(SyncQueueItem record)
        {
            try
            {
                _logger.LogDebug("Processing CashReceipt {Id}...", record.Id);

                await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Processing);

                var cashReceipt = JsonSerializer.Deserialize<CashReceiptDto>(record.Payload);
                if (cashReceipt == null)
                {
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, "Payload parse error");
                    return false;
                }

                var slip = MapToSafeDepositSlip(cashReceipt);
                var requestJson = JsonSerializer.Serialize(slip, _jsonOptions);
                await _repository.LogAsync(record.Id, "INFO", "Sending CashReceipt to jPlatform", requestJson);

                var response = await _apiClient.CreateCashReceiptAsync(slip);

                if (response.Success)
                {
                    var refValue = response.TransactionNo ?? response.LogicalRef?.ToString();
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Success, null, refValue);
                    await _repository.LogAsync(record.Id, "INFO", $"CashReceipt Success. TransactionNo: {response.TransactionNo}", requestJson, response.Data);
                    _logger.LogInformation("CashReceipt {Id} synced. TransactionNo: {TransNo}", record.Id, response.TransactionNo);
                    return true;
                }
                else
                {
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, response.ErrorMessage);
                    await _repository.LogAsync(record.Id, "ERROR", response.ErrorMessage, requestJson, response.Data);
                    _logger.LogWarning("CashReceipt {Id} failed: {Error}", record.Id, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, ex.Message);
                await _repository.LogAsync(record.Id, "ERROR", ex.Message);
                _logger.LogError(ex, "Error processing CashReceipt {Id}", record.Id);
                return false;
            }
        }

        /// <summary>
        /// Çek Giriş Bordrosu işle (ChequeReceipt)
        /// </summary>
        private async Task<bool> ProcessChequeRecordAsync(SyncQueueItem record)
        {
            try
            {
                _logger.LogDebug("Processing ChequeReceipt {Id}...", record.Id);

                await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Processing);

                var cashReceipt = JsonSerializer.Deserialize<CashReceiptDto>(record.Payload);
                if (cashReceipt == null)
                {
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, "Payload parse error");
                    return false;
                }

                var slip = MapToChequeSlip(cashReceipt);
                var requestJson = JsonSerializer.Serialize(slip, _jsonOptions);
                await _repository.LogAsync(record.Id, "INFO", "Sending ChequeReceipt to jPlatform", requestJson);

                var response = await _apiClient.CreateChequeReceiptAsync(slip);

                if (response.Success)
                {
                    var refValue = response.TransactionNo ?? response.LogicalRef?.ToString();
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Success, null, refValue);
                    await _repository.LogAsync(record.Id, "INFO", $"ChequeReceipt Success. TransactionNo: {response.TransactionNo}", requestJson, response.Data);
                    _logger.LogInformation("ChequeReceipt {Id} synced. TransactionNo: {TransNo}", record.Id, response.TransactionNo);
                    return true;
                }
                else
                {
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, response.ErrorMessage);
                    await _repository.LogAsync(record.Id, "ERROR", response.ErrorMessage, requestJson, response.Data);
                    _logger.LogWarning("ChequeReceipt {Id} failed: {Error}", record.Id, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, ex.Message);
                await _repository.LogAsync(record.Id, "ERROR", ex.Message);
                _logger.LogError(ex, "Error processing ChequeReceipt {Id}", record.Id);
                return false;
            }
        }

        /// <summary>
        /// Sipariş işle (Order)
        /// SRC_Orders + SRC_OrderDetails → Logo /salesOrder
        /// </summary>
        private async Task<bool> ProcessOrderRecordAsync(SyncQueueItem record)
        {
            try
            {
                _logger.LogDebug("Processing Order {Id}...", record.Id);

                await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Processing);

                // 1. EntityId'den OrderId al
                if (!long.TryParse(record.EntityId, out var orderId))
                {
                    _logger.LogError("Order {Id}: Invalid EntityId '{EntityId}'", record.Id, record.EntityId);
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, "Invalid EntityId");
                    return false;
                }

                // 2. Header + Details birlikte getir
                var order = await _repository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    _logger.LogError("Order {Id}: OrderId {OrderId} not found in SRC_Orders", record.Id, orderId);
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, "Order not found");
                    return false;
                }

                if (order.Details == null || order.Details.Count == 0)
                {
                    _logger.LogError("Order {Id}: OrderId {OrderId} has no detail lines", record.Id, orderId);
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, "No order details");
                    return false;
                }

                _logger.LogInformation("Order {Id}: FisNo={FisNo}, Customer={Customer}, Lines={LineCount}",
                    record.Id, order.FisNo, order.CustomerCode, order.Details.Count);

                // 3. Cari kodundan ödeme planını getir
                var paymentPlan = await _repository.GetPaymentPlanByCustomerAsync(order.CustomerCode);
                if (!string.IsNullOrEmpty(paymentPlan))
                {
                    order.PaymentPlan = paymentPlan;
                    _logger.LogDebug("Order {Id}: PaymentPlan={PaymentPlan}", record.Id, paymentPlan);
                }

                // 4. Logo J-Platform formatına dönüştür
                var orderSlip = OrderMapper.MapToJplatformOrder(order);
                var requestJson = JsonSerializer.Serialize(orderSlip, _jsonOptions);
                await _repository.LogAsync(record.Id, "INFO", "Sending Order to jPlatform", requestJson);

                // 5. Logo'ya gönder
                var response = await _apiClient.SendOrderAsync(orderSlip);

                if (response.Success)
                {
                    var refValue = response.TransactionNo ?? response.LogicalRef?.ToString();
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Success, null, refValue);
                    await _repository.LogAsync(record.Id, "INFO", $"Order Success. TransactionNo: {response.TransactionNo}", requestJson, response.Data);
                    _logger.LogInformation("Order {Id} synced. TransactionNo: {TransNo}", record.Id, response.TransactionNo);
                    return true;
                }
                else
                {
                    await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, response.ErrorMessage);
                    await _repository.LogAsync(record.Id, "ERROR", response.ErrorMessage, requestJson, response.Data);
                    _logger.LogWarning("Order {Id} failed: {Error}", record.Id, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _repository.UpdateSyncStatusAsync(record.Id, SyncStatus.Failed, ex.Message);
                await _repository.LogAsync(record.Id, "ERROR", ex.Message);
                _logger.LogError(ex, "Error processing Order {Id}", record.Id);
                return false;
            }
        }

        /// <summary>
        /// Sanal POS → Cari Hesap Fişi (ArpSlip)
        /// PUNTO View: PNTV_ERYAZ_SANALPOS_AKTILACAKLAR → POST /arpslips?slipType=08
        /// Başarılı: ERYAZ_SANALPOS.AKTARIM = 50
        /// </summary>
        private async Task ProcessSanalPosAsync(CancellationToken cancellationToken)
        {
            try
            {
                var pendingRecords = await _puntoRepository.GetPendingSanalPosAsync(_batchSize);

                if (pendingRecords == null || pendingRecords.Count == 0)
                {
                    _logger.LogDebug("No pending SanalPos records");
                    return;
                }

                _logger.LogInformation("Processing {Count} SanalPos records...", pendingRecords.Count);

                int successCount = 0;
                int failCount = 0;

                foreach (var record in pendingRecords)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // 1. Temsilci kodunu dönüştür
                        record.PlasiyerKodu = _salespersonHelper.Resolve(record.PlasiyerKodu);

                        // 2. DTO → Logo ArpSlip modeline dönüştür
                        var arpSlip = SanalPosMapper.MapToArpSlip(record);

                        var requestJson = JsonSerializer.Serialize(arpSlip, _jsonOptions);
                        _logger.LogDebug("SanalPos ID={Id}, BelgeNo={BelgeNo} - Request: {Json}",
                            record.Id, record.BelgeNo, requestJson);

                        // 2. Logo'ya gönder
                        var response = await _apiClient.CreateArpSlipAsync(arpSlip);

                        if (response.Success)
                        {
                            _logger.LogInformation(
                                "SanalPos ID={Id}, BelgeNo={BelgeNo}: Synced. TxNo: {TxNo}",
                                record.Id, record.BelgeNo, response.TransactionNo);

                            // 3. PUNTO'da AKTARIM=50 olarak işaretle
                            await _puntoRepository.UpdateSanalPosAktarimAsync(record.Id);
                            successCount++;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "SanalPos ID={Id}, BelgeNo={BelgeNo}: Failed. Error: {Error}",
                                record.Id, record.BelgeNo, response.ErrorMessage);
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "SanalPos ID={Id}, BelgeNo={BelgeNo}: Processing error",
                            record.Id, record.BelgeNo);
                        failCount++;
                    }
                }

                _logger.LogInformation(
                    "SanalPos batch completed. Success: {Success}, Failed: {Failed}",
                    successCount, failCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SanalPos processing error");
            }
        }

        #region Sales Invoice Processing (İrsaliyeden Faturalama)

        /// <summary>
        /// İrsaliyeden faturalama döngüsü.
        /// PUNTO View: PNTV_005_IrsaliyeDetay_Faturala (BILLED=0, BILLSTATUS=0)
        /// CARI_KOD bazında grupla → her grup tek fatura
        /// POST /invoices/sales?invoiceType=8
        /// </summary>
        private async Task ProcessSalesInvoicesAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bekleyen satırları oku
                var pendingRows = await _puntoRepository.GetPendingInvoiceDetailsAsync();

                if (pendingRows == null || pendingRows.Count == 0)
                {
                    _logger.LogDebug("No pending SalesInvoice records");
                    return;
                }

                _logger.LogInformation("SalesInvoice: {Count} satır bulundu, CARI_KOD bazında gruplanıyor...", pendingRows.Count);

                // 2. CARI_KOD bazında grupla
                var groups = SalesInvoiceMapper.GroupByCari(pendingRows);
                _logger.LogInformation("SalesInvoice: {GroupCount} ayrı cari için fatura oluşturulacak.", groups.Count);

                int successCount = 0;
                int failCount = 0;

                foreach (var group in groups)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var result = await ProcessSalesInvoiceGroupAsync(group);
                        if (result)
                            successCount++;
                        else
                            failCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex,
                            "SalesInvoice HATA: Cari={CariKod}, Satır={RowCount}",
                            group.CariKod, group.Rows.Count);
                    }
                }

                _logger.LogInformation(
                    "SalesInvoice batch completed. Success: {Success}, Failed: {Failed} / {Total} total",
                    successCount, failCount, groups.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesInvoice processing error");
            }
        }

        /// <summary>
        /// Tek bir CARI_KOD grubu için fatura oluştur ve Logo'ya gönder.
        /// </summary>
        private async Task<bool> ProcessSalesInvoiceGroupAsync(SalesInvoiceGroup group)
        {
            var cariKod = group.CariKod;
            var rows = group.Rows;
            var materialRows = rows.Where(r => r.LineType == 0).ToList();

            _logger.LogInformation(
                "SalesInvoice: Cari={CariKod} ({CariAd}), MalzemeSatır={LineCount}, İrsaliye={DispatchCount}",
                cariKod,
                rows.First().Cari,
                materialRows.Count,
                rows.Select(r => r.IrsaliyeNo).Distinct().Count());

            // 3. Mapping: View satırları → Logo JSON modeli
            var invoice = SalesInvoiceMapper.MapToInvoice(group);

            // Temsilci kodunu dönüştür
            invoice.SalespersonCode = _salespersonHelper.Resolve(invoice.SalespersonCode);
            foreach (var item in invoice.ItemTransactionDTO)
            {
                item.PurchaseEmployeeSalespersonCode = _salespersonHelper.Resolve(item.PurchaseEmployeeSalespersonCode);
            }

            var requestJson = JsonSerializer.Serialize(invoice, _jsonOptions);
            _logger.LogDebug("SalesInvoice: Cari={CariKod} - Request: {Json}", cariKod, requestJson);

            // 4. Logo'ya gönder
            var response = await _apiClient.CreateSalesInvoiceAsync(invoice);

            if (response.Success)
            {
                _logger.LogInformation(
                    "SalesInvoice BAŞARILI: Cari={CariKod}, LogoRef={LogicalRef}, Code={Code}, Part1={Part1}, Part2={Part2}",
                    cariKod, response.LogicalRef, response.Code, response.InvoiceNoPart1, response.InvoiceNoPart2);

                // 5. Kampanya uygula (code varsa)
                if (!string.IsNullOrEmpty(response.Code))
                {
                    var campaignRequest = new ApplyCampaignRequest
                    {
                        No = response.Code,
                        Date = invoice.Date,
                        OrgUnit = invoice.OrgUnit,
                        Warehouse = invoice.Warehouse,
                        Arap = invoice.Arap
                    };

                    var campaignResponse = await _apiClient.ApplyCampaignAsync(campaignRequest);

                    if (campaignResponse.Success)
                    {
                        _logger.LogInformation(
                            "ApplyCampaign BAŞARILI: Cari={CariKod}, FaturaNo={Code}",
                            cariKod, response.Code);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "ApplyCampaign BAŞARISIZ: Cari={CariKod}, FaturaNo={Code}, Hata={Error}",
                            cariKod, response.Code, campaignResponse.ErrorMessage);
                    }
                }

                // 6. PUNTO'da faturalanan satırları işaretle
                var satırRefList = rows
                    .Where(r => r.LineType == 0)
                    .Select(r => r.IrsaliyeSatirRef)
                    .ToList();

                await _puntoRepository.MarkInvoiceDetailAsBilledAsync(satırRefList, response.LogicalRef ?? 0);
                return true;
            }
            else
            {
                _logger.LogWarning(
                    "SalesInvoice BAŞARISIZ: Cari={CariKod}, Hata={Error}",
                    cariKod, response.ErrorMessage);
                return false;
            }
        }

        #endregion

        #region Mapping Methods

        /// <summary>
        /// Nakit Tahsilat → Kasa Fişi (SafeDepositSlip)
        /// </summary>
        private JplatformSafeDepositSlip MapToSafeDepositSlip(CashReceiptDto dto)
        {
            var receiptDateTime = dto.ReceiptDate.Date.Add(DateTime.Now.TimeOfDay);
            var dateString = receiptDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "+03:00";

            return new JplatformSafeDepositSlip
            {
                PCPointCode = dto.CashAccountCode,
                SafeDepositCode = dto.CashAccountCode,
                Date = dateString,
                Hour = new HourModel
                {
                    Hour = receiptDateTime.Hour,
                    Minute = receiptDateTime.Minute,
                    Second = 0,
                    Milisecond = 0
                },
                Transaction = new List<object>(),
                Transaction3 = new List<object>(),
                Transaction4 = new List<object>(),
                Transaction2 = new List<SafeDepositTransaction>
                {
                    new SafeDepositTransaction
                    {
                        Type = TransactionTypes.NakitTahsilat,
                        TransactionNumber = dto.ReceiptNo,
                        DocumentNo = dto.ReceiptNo,
                        DocumentDate = dateString,
                        Description = dto.Description ?? "",
                        ARaPCode = dto.CustomerCode,
                        Amount = dto.Amount,
                        TcType = GetTcType(dto.CurrencyCode),
                        AmountTC = dto.CurrencyCode != "TRY" ? dto.Amount : 0,
                        TcExchangeRate = dto.CurrencyCode != "TRY" ? dto.ExchangeRate : 0,
                        AuxCode = "",
                        AuthCode = "",
                        ARaPBranches = "",
                        ARaPBranchesDescription = "",
                        TradingGroup = "",
                        ContractNumber = "",
                        ImportExportFileCode = "",
                        BankAccountCode = "",
                        BankAccountName = "",
                        PCPointCode = "",
                        PCPointDescription = "",
                        PayrollPayment = "",
                        PayrollPaymentDescription = "",
                        GLAccountCode = "",
                        GLAccountName = "",
                        RegistryNumber = "",
                        Name2 = "",
                        Surname2 = "",
                        OrgUnit = "",
                        CustomerName = dto.CustomerName ?? "",
                        CustomerSurname = "",
                        RegistrationNumber = "",
                        Address = "",
                        Name = "",
                        Surname = "",
                        TrIdentificationNoorTaxNo = "",
                        Address2 = "",
                        ComboboxReturn = 0,
                        TAccount = 0,
                        TDistributionRate = 0,
                        TAmount = 0,
                        TAmountRC = 0,
                        TAmountTC = 0,
                        OperationCategory = 0,
                        ServiceType = 0,
                        StoppageRate = 0,
                        StoppageRateAmount = 0,
                        FundShareRate = 0,
                        FundShareRateAmount = 0,
                        Total = 0,
                        StoppageRate2 = 0,
                        StoppageRateAmount2 = 0,
                        DeductionWillBeApplied = 0,
                        FundShareRate2 = 0,
                        FundShareRateAmount2 = 0,
                        DeductionWillBeDistributed = 0,
                        VatOnGross = 0,
                        VatRateAmount = 0,
                        DeductionRate = 0,
                        DeductionRate2 = 0,
                        DeductionsTotal = 0,
                        GrossFee = 0,
                        NetFee = 0,
                        VatRate = 0,
                        VatAmount = 0,
                        VatInclusiveAmount = dto.Amount,
                        TotalLocalCurrency = 0,
                        AverageDay = 0,
                        NumberOfRecords = 0,
                        RemainingAmountInLC = 0,
                        RemainingAmountInRC = 0,
                        RemainingAmountInFC = 0,
                        TotalInLC = 0,
                        TotalInRC = 0,
                        TotalInFC = 0,
                        RemainingRate = 0,
                        Index = 0,
                        PrivateCompany = false,
                        VatIncluded = false,
                        NoteTransaction = new List<object>()
                    }
                },
                Extensions = new ExtensionsModel { List = new List<object>() },
                Index = 0
            };
        }

        /// <summary>
        /// Çek Giriş → Çek/Senet Bordrosu (ChequePNoteSlip)
        /// </summary>
        private JplatformChequePNoteSlip MapToChequeSlip(CashReceiptDto dto)
        {
            var slipDate = dto.ReceiptDate.ToString("yyyy-MM-ddT00:00:00.000") + "+03:00";
            var dueDate = (dto.DueDate ?? dto.ReceiptDate).ToString("yyyy-MM-ddT00:00:00.000") + "+03:00";

            // Çek/Senet numarası - yoksa ReceiptNo kullan
            var chequeNo = !string.IsNullOrEmpty(dto.ChequeNo) ? dto.ChequeNo : dto.ReceiptNo;

            return new JplatformChequePNoteSlip
            {
                SlipDate = slipDate,
                OrgUnit = _configuration.GetValue("ChequeSettings:OrgUnit", "01"),
                OrgUnitDescription = _configuration.GetValue("ChequeSettings:OrgUnitDescription", "MERKEZ"),
                ArapCode = dto.CustomerCode,
                ArapTitle = dto.CustomerName ?? "",
                SalespersonCode = "",
                NoteTransaction = new List<ChequeNoteTransaction>
                {
                    new ChequeNoteTransaction
                    {
                        PortfolioNo = chequeNo,
                        SerialNo = chequeNo,
                        DueDate = dueDate,
                        Debtor = dto.CustomerName ?? "",
                        PlaceOfPayment = "",
                        BankName = dto.BankName ?? "",
                        Amount = dto.Amount,
                        ChequePromNoteTransaction = new List<object>(),
                        Index = 0
                    }
                },
                Notes = dto.Description ?? "",
                TotalLocalCurrency = dto.Amount,
                AvergDueDate = dueDate,
                NumberOfRecords = 1,
                BankTransaction = new List<BankTransactionItem> { new BankTransactionItem { Index = 0 } },
                AnalysisTransaction = new List<AnalysisTransactionItem> { new AnalysisTransactionItem { Index = 0 } },
                RemainingRate = 100,
                MainChartOfAccounts = "04",
                MainChartOfAccountsDesc = "Günlük / Mahsup",
                Index = 0
            };
        }

        private int GetTcType(string currencyCode)
        {
            return currencyCode?.ToUpper() switch
            {
                "TRY" => 0,
                "USD" => 1,
                "EUR" => 20,
                "GBP" => 2,
                _ => 0
            };
        }

        #endregion
    }
}
