using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogoSync.Core.DTOs
{
    // =========================================================================
    // Logo J-Platform REST API - Satış Faturası (İrsaliyeden Faturalama)
    // POST /invoices/sales?invoiceType=8
    //
    // Kaynak: PUNTO.dbo.PNTV_005_IrsaliyeDetay_Faturala
    // Gruplama: CARI_KOD bazında → aynı müşterinin tüm irsaliye satırları tek fatura
    // =========================================================================

    /// <summary>
    /// Ana fatura modeli - Header + nested DTO'lar
    /// </summary>
    public class JplatformSalesInvoice
    {
        // ── Satış Elemanı ──────────────────────────────────────────────
        /// <summary>Kaynak: SATIS_ELEMANI</summary>
        [JsonPropertyName("salespersonCode")]
        public string SalespersonCode { get; set; }

        // ── Malzeme Satırları ──────────────────────────────────────────
        /// <summary>
        /// Fatura kalemleri dizisi.
        /// LINETYPE=0 → malzeme satırı (deep=false)
        /// LINETYPE=2 → iskonto satırı (deep=false, percent=DISCPER)
        /// </summary>
        [JsonPropertyName("itemTransactionDTO")]
        public List<SalesInvoiceItemTransaction> ItemTransactionDTO { get; set; } = new List<SalesInvoiceItemTransaction>();

        // ── İrsaliye Referansları ──────────────────────────────────────
        /// <summary>
        /// Benzersiz irsaliye başlıkları (IRSALIYE_NO bazında distinct)
        /// </summary>
        [JsonPropertyName("masterDataDispatcDTO")]
        public List<SalesInvoiceMasterDataDispatch> MasterDataDispatcDTO { get; set; } = new List<SalesInvoiceMasterDataDispatch>();

        // ── e-İrsaliye Detayları (boş) ────────────────────────────────
        [JsonPropertyName("eWayInfoDetailsDTO")]
        public List<object> EWayInfoDetailsDTO { get; set; } = new List<object>();

        // ── Ödeme Taksitleri ──────────────────────────────────────────
        /// <summary>
        /// Ödeme planı taksitleri.
        /// amount = Toplam tutar + KDV (SUM(MIKTAR*FIYAT) * (1 + VATRATE/100))
        /// </summary>
        [JsonPropertyName("installmentDTO")]
        public List<SalesInvoiceInstallment> InstallmentDTO { get; set; } = new List<SalesInvoiceInstallment>();

        // ── Header Alanları ───────────────────────────────────────────

        /// <summary>Kaynak: BOSTATUS (genelde 1)</summary>
        [JsonPropertyName("boStatus")]
        public int BoStatus { get; set; } = 1;

        /// <summary>Fatura seri kodu (sabit "PNT")</summary>
        [JsonPropertyName("serialOrderNoPart1")]
        public string SerialOrderNoPart1 { get; set; } = "PNT";

        /// <summary>Fatura sıra numarası (sabit "GUNCELLE" — Logo kayıt sonrası gerçek değer atanır)</summary>
        [JsonPropertyName("serialOrderNoPart2")]
        public string SerialOrderNoPart2 { get; set; } = "GUNCELLE";

        /// <summary>Fatura tarihi. Logo format: "2026-02-20T10:50:47.000+03:00"</summary>
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("time")]
        public SalesInvoiceTime Time { get; set; }

        /// <summary>Belge tarihi. Logo format</summary>
        [JsonPropertyName("documentDate")]
        public string DocumentDate { get; set; }

        /// <summary>Kaynak: ISYERI → "01.7"</summary>
        [JsonPropertyName("orgUnit")]
        public string OrgUnit { get; set; }

        /// <summary>Kaynak: AMBAR → "01.7.7"</summary>
        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; }

        /// <summary>Kaynak: CARI_KOD</summary>
        [JsonPropertyName("arap")]
        public string Arap { get; set; }

        /// <summary>Kaynak: CARI</summary>
        [JsonPropertyName("araptitle")]
        public string ArapTitle { get; set; }

        /// <summary>Kaynak: CARI</summary>
        [JsonPropertyName("araptitle2")]
        public string ArapTitle2 { get; set; }

        /// <summary>Kaynak: CARI</summary>
        [JsonPropertyName("araptitle3")]
        public string ArapTitle3 { get; set; }

        /// <summary>Kaynak: ODEME_PLANI</summary>
        [JsonPropertyName("paymentPlan")]
        public string PaymentPlan { get; set; }

        /// <summary>Kaynak: ODEME_PLANI</summary>
        [JsonPropertyName("paymentPlan2")]
        public string PaymentPlan2 { get; set; }

        [JsonPropertyName("eInvoiceeArchiveType")]
        public int EInvoiceeArchiveType { get; set; } = 0;

        [JsonPropertyName("eInvoice")]
        public bool EInvoice { get; set; } = false;

        [JsonPropertyName("eInvoice2")]
        public bool EInvoice2 { get; set; } = true;

        [JsonPropertyName("eArchive")]
        public bool EArchive { get; set; } = false;

        [JsonPropertyName("eArchive2")]
        public bool EArchive2 { get; set; } = false;

        [JsonPropertyName("onlineSalesInvoice")]
        public bool OnlineSalesInvoice { get; set; } = false;

        [JsonPropertyName("onlineSalesInvoice2")]
        public bool OnlineSalesInvoice2 { get; set; } = false;

        [JsonPropertyName("electronicDocument")]
        public bool ElectronicDocument { get; set; } = true;

        [JsonPropertyName("electronicDocument2")]
        public bool ElectronicDocument2 { get; set; } = false;

        [JsonPropertyName("electronicDocument3")]
        public bool ElectronicDocument3 { get; set; } = true;

        [JsonPropertyName("expenseSheet")]
        public bool ExpenseSheet { get; set; } = false;

        [JsonPropertyName("rcexchangeRate")]
        public decimal RcExchangeRate { get; set; } = 0;

        /// <summary>Kaynak: CARI_KOD</summary>
        [JsonPropertyName("customer")]
        public string Customer { get; set; }

        /// <summary>Kaynak: CARI_KOD</summary>
        [JsonPropertyName("customer2")]
        public string Customer2 { get; set; }

        /// <summary>Vergi numarası</summary>
        [JsonPropertyName("taxNo")]
        public string TaxNo { get; set; } = "";

        [JsonPropertyName("totalReportingCurrency")]
        public decimal TotalReportingCurrency { get; set; } = 0;

        [JsonPropertyName("generalCurrency")]
        public int GeneralCurrency { get; set; } = 1;

        [JsonPropertyName("linesCurrency")]
        public int LinesCurrency { get; set; } = 1;

        [JsonPropertyName("referenceDate")]
        public string ReferenceDate { get; set; }

        [JsonPropertyName("remainingRate")]
        public decimal RemainingRate { get; set; } = 100.0m;

        [JsonPropertyName("mainChartofAccounts")]
        public string MainChartofAccounts { get; set; } = "04";

        [JsonPropertyName("mainChartofAccountsDesc")]
        public string MainChartofAccountsDesc { get; set; } = "Günlük / Mahsup";

        [JsonPropertyName("distributeDiscounts")]
        public bool DistributeDiscounts { get; set; } = true;

        [JsonPropertyName("distributePromotions")]
        public bool DistributePromotions { get; set; } = true;

        [JsonPropertyName("distributeExpenses")]
        public bool DistributeExpenses { get; set; } = false;

        [JsonPropertyName("distributeReverseCharge")]
        public bool DistributeReverseCharge { get; set; } = true;

        /// <summary>Kaynak: CARI_KOD</summary>
        [JsonPropertyName("codeShipTo")]
        public string CodeShipTo { get; set; }

        [JsonPropertyName("scenario2")]
        public int Scenario2 { get; set; } = 1;

        [JsonPropertyName("cashRegisterSlip")]
        public bool CashRegisterSlip { get; set; } = false;

        [JsonPropertyName("substitutesDispatchReceipt")]
        public bool SubstitutesDispatchReceipt { get; set; } = true;

        [JsonPropertyName("infoSlipType")]
        public int InfoSlipType { get; set; } = 0;

        [JsonPropertyName("invoiceNumber")]
        public int InvoiceNumber { get; set; } = 0;

        [JsonPropertyName("invoiceType")]
        public int InvoiceType { get; set; } = 0;

        [JsonPropertyName("paymentplanDescription")]
        public string PaymentPlanDescription { get; set; } = "";

        /// <summary>Kaynak: ISYERI</summary>
        [JsonPropertyName("orgunit2")]
        public string OrgUnit2 { get; set; }

        /// <summary>Kaynak: AMBAR</summary>
        [JsonPropertyName("warehouse2")]
        public string Warehouse2 { get; set; }

        [JsonPropertyName("PaymentAmountDifference")]
        public decimal PaymentAmountDifference { get; set; } = 0;

        [JsonPropertyName("PaymentAmountDifference2")]
        public decimal PaymentAmountDifference2 { get; set; } = 0;

        [JsonPropertyName("exchangeRateDifference")]
        public bool ExchangeRateDifference { get; set; } = false;

        [JsonPropertyName("reverseChargeApplicability")]
        public int ReverseChargeApplicability { get; set; } = -1;

        [JsonPropertyName("applicablePercentofTaxRate")]
        public decimal ApplicablePercentofTaxRate { get; set; } = 100.0m;

        /// <summary>Kaynak: CARI</summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("paper")]
        public bool Paper { get; set; } = false;

        [JsonPropertyName("paper2")]
        public bool Paper2 { get; set; } = false;

        [JsonPropertyName("useAsShippingAddress")]
        public bool UseAsShippingAddress { get; set; } = false;

        [JsonPropertyName("eProducer")]
        public bool EProducer { get; set; } = false;

        [JsonPropertyName("tourist")]
        public bool Tourist { get; set; } = false;

        [JsonPropertyName("legalEntity")]
        public bool LegalEntity { get; set; } = false;

        [JsonPropertyName("iBAN")]
        public int IBAN { get; set; } = 0;

        [JsonPropertyName("new1")]
        public bool New1 { get; set; } = false;

        [JsonPropertyName("new2")]
        public bool New2 { get; set; } = false;

        [JsonPropertyName("new3")]
        public bool New3 { get; set; } = false;

        [JsonPropertyName("integrationType")]
        public int IntegrationType { get; set; } = 0;

        [JsonPropertyName("eArchiveGIB")]
        public bool EArchiveGIB { get; set; } = false;

        [JsonPropertyName("edispatchDetails")]
        public List<object> EdispatchDetails { get; set; } = new List<object>();

        [JsonPropertyName("extensions")]
        public SalesInvoiceExtensions Extensions { get; set; } = new SalesInvoiceExtensions();

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    // =========================================================================
    // itemTransactionDTO - Malzeme / İskonto Satırı
    // =========================================================================

    /// <summary>
    /// Fatura satır kalemi.
    /// LINETYPE=0 → Malzeme satırı: deep=false, code=URUN_KODU, quantity=MIKTAR, unitPrice=FIYAT
    /// LINETYPE=2 → İskonto satırı: deep=false, percent=DISCPER
    /// </summary>
    public class SalesInvoiceItemTransaction
    {
        /// <summary>
        /// Malzeme ve iskonto satırlarında false
        /// </summary>
        [JsonPropertyName("deep")]
        public bool Deep { get; set; } = false;

        /// <summary>Kaynak: IRSALIYE_SATIR_REF</summary>
        [JsonPropertyName("logicalRef")]
        public long LogicalRef { get; set; }

        [JsonPropertyName("sourceRef")]
        public long SourceRef { get; set; } = 0;

        /// <summary>Kaynak: SIPARIS_SATIR_REF (orderTransRef)</summary>
        [JsonPropertyName("orderTransRef")]
        public long OrderTransRef { get; set; }

        /// <summary>Kaynak: SIPARIS_REF</summary>
        [JsonPropertyName("orderSlipRef")]
        public long OrderSlipRef { get; set; }

        /// <summary>Kaynak: IRSALIYE_REF</summary>
        [JsonPropertyName("dispatchRef")]
        public long DispatchRef { get; set; }

        /// <summary>Kaynak: IRSALIYE_SATIR_REF (dispatchTransRef)</summary>
        [JsonPropertyName("dispatchTransRef")]
        public long DispatchTransRef { get; set; }

        /// <summary>0=malzeme, 2=iskonto</summary>
        [JsonPropertyName("type")]
        public int Type { get; set; }

        /// <summary>Kaynak: URUN_KODU (malzeme satırında)</summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }

        /// <summary>Kaynak: URUN (ürün açıklaması)</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("variantCode")]
        public string VariantCode { get; set; } = "";

        /// <summary>Kaynak: MIKTAR</summary>
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>Birim tipi (29 = ADET)</summary>
        [JsonPropertyName("unit")]
        public int Unit { get; set; }

        /// <summary>Kaynak: BIRIM</summary>
        [JsonPropertyName("unitCode")]
        public string UnitCode { get; set; }

        /// <summary>Kaynak: FIYAT</summary>
        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; } = 0;

        [JsonPropertyName("unitCostIFRS")]
        public decimal UnitCostIFRS { get; set; } = 0;

        [JsonPropertyName("currencyTypeRC")]
        public int CurrencyTypeRC { get; set; } = 1;

        [JsonPropertyName("unitPriceInFCurrTC")]
        public decimal UnitPriceInFCurrTC { get; set; } = 0;

        /// <summary>Kaynak: FIYAT_KUR</summary>
        [JsonPropertyName("currencyPC")]
        public int CurrencyPC { get; set; } = 0;

        [JsonPropertyName("exchRate")]
        public decimal ExchRate { get; set; } = 0;

        [JsonPropertyName("unitPricePC")]
        public decimal UnitPricePC { get; set; } = 0;

        /// <summary>
        /// LINETYPE=0 → 0.0 (malzeme satırında)
        /// LINETYPE=2 → DISCPER değeri (iskonto satırında)
        /// </summary>
        [JsonPropertyName("percent")]
        public decimal Percent { get; set; } = 0;

        [JsonPropertyName("netDiscount")]
        public bool NetDiscount { get; set; } = false;

        [JsonPropertyName("netDiscountPercent")]
        public decimal NetDiscountPercent { get; set; } = 0;

        [JsonPropertyName("netDiscountAmount")]
        public decimal NetDiscountAmount { get; set; } = 0;

        [JsonPropertyName("netDiscountAmountRC")]
        public decimal NetDiscountAmountRC { get; set; } = 0;

        /// <summary>Kaynak: VATRATE</summary>
        [JsonPropertyName("vatratePercent")]
        public decimal VatratePercent { get; set; }

        [JsonPropertyName("vatincluded")]
        public bool VatIncluded { get; set; } = false;

        /// <summary>KDV tutarı (Logo tarafından hesaplanır)</summary>
        [JsonPropertyName("vatamount")]
        public decimal VatAmount { get; set; } = 0;

        /// <summary>KDV matrahı (Logo tarafından hesaplanır)</summary>
        [JsonPropertyName("vatbase")]
        public decimal VatBase { get; set; } = 0;

        [JsonPropertyName("gstincluded")]
        public bool GstIncluded { get; set; } = false;

        [JsonPropertyName("cgstrate")]
        public decimal CgstRate { get; set; } = 0;

        [JsonPropertyName("cgstamount")]
        public decimal CgstAmount { get; set; } = 0;

        [JsonPropertyName("cgstbase")]
        public decimal CgstBase { get; set; } = 0;

        [JsonPropertyName("sgstrate")]
        public decimal SgstRate { get; set; } = 0;

        [JsonPropertyName("sgstamount")]
        public decimal SgstAmount { get; set; } = 0;

        [JsonPropertyName("sgstbase")]
        public decimal SgstBase { get; set; } = 0;

        [JsonPropertyName("igstrate")]
        public decimal IgstRate { get; set; } = 0;

        [JsonPropertyName("igstamount")]
        public decimal IgstAmount { get; set; } = 0;

        [JsonPropertyName("ugstrate")]
        public decimal UgstRate { get; set; } = 0;

        [JsonPropertyName("ugstamount")]
        public decimal UgstAmount { get; set; } = 0;

        [JsonPropertyName("igstbase")]
        public decimal IgstBase { get; set; } = 0;

        /// <summary>Kaynak: TOTAL = MIKTAR * FIYAT</summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("amountPC")]
        public decimal AmountPC { get; set; } = 0;

        [JsonPropertyName("amountinFCurrency")]
        public decimal AmountInFCurrency { get; set; } = 0;

        /// <summary>Net tutar (iskonto sonrası)</summary>
        [JsonPropertyName("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonPropertyName("cessbase")]
        public decimal CessBase { get; set; } = 0;

        [JsonPropertyName("cessrate")]
        public decimal CessRate { get; set; } = 0;

        [JsonPropertyName("cesstotal")]
        public decimal CessTotal { get; set; } = 0;

        [JsonPropertyName("additionalTaxBase")]
        public decimal AdditionalTaxBase { get; set; } = 0;

        [JsonPropertyName("additionalTaxRatePercent")]
        public decimal AdditionalTaxRatePercent { get; set; } = 0;

        [JsonPropertyName("additionalTaxAmount")]
        public decimal AdditionalTaxAmount { get; set; } = 0;

        [JsonPropertyName("additionalTaxAmountRC")]
        public decimal AdditionalTaxAmountRC { get; set; } = 0;

        [JsonPropertyName("costType")]
        public int CostType { get; set; } = -1;

        [JsonPropertyName("transactionAuxCode")]
        public string TransactionAuxCode { get; set; } = "";

        [JsonPropertyName("transactionAuxCodeDescription")]
        public string TransactionAuxCodeDescription { get; set; } = "";

        [JsonPropertyName("transactionAuxCode2")]
        public string TransactionAuxCode2 { get; set; } = "";

        [JsonPropertyName("transactionAuxCodeDescription2")]
        public string TransactionAuxCodeDescription2 { get; set; } = "";

        [JsonPropertyName("deliveryCode")]
        public string DeliveryCode { get; set; } = "";

        [JsonPropertyName("commissionPercent")]
        public decimal CommissionPercent { get; set; } = 0;

        /// <summary>Kaynak: SIPARIS_NO</summary>
        [JsonPropertyName("orderSlipNumber")]
        public string OrderSlipNumber { get; set; }

        [JsonPropertyName("prepaidOrder")]
        public bool PrepaidOrder { get; set; } = false;

        /// <summary>Kaynak: SIPARIS_TARIHI (Logo format)</summary>
        [JsonPropertyName("orderDate")]
        public string OrderDate { get; set; }

        /// <summary>Kaynak: SATIR_ODEME_PLANI (NULL ise boş string)</summary>
        [JsonPropertyName("paymentPlan")]
        public string PaymentPlan { get; set; } = "";

        /// <summary>Kaynak: SATIS_ELEMANI</summary>
        [JsonPropertyName("purchaseEmployeeSalespersonCode")]
        public string PurchaseEmployeeSalespersonCode { get; set; }

        [JsonPropertyName("outputLogNumber")]
        public string OutputLogNumber { get; set; } = "";

        [JsonPropertyName("description2")]
        public string Description2 { get; set; } = "";

        [JsonPropertyName("priceCode")]
        public string PriceCode { get; set; } = "";

        /// <summary>Kaynak: AMBAR</summary>
        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; }

        [JsonPropertyName("distributionType")]
        public int DistributionType { get; set; } = -1;

        [JsonPropertyName("distributionType2")]
        public int DistributionType2 { get; set; } = 0;

        [JsonPropertyName("contractNo")]
        public string ContractNo { get; set; } = "";

        /// <summary>Kaynak: IRSALIYE_NO</summary>
        [JsonPropertyName("dispatchNo")]
        public string DispatchNo { get; set; }

        [JsonPropertyName("reverseChargeNumerator")]
        public decimal ReverseChargeNumerator { get; set; } = 0;

        [JsonPropertyName("reverseChargeDenominator")]
        public decimal ReverseChargeDenominator { get; set; } = 0;

        [JsonPropertyName("deductionNumerator")]
        public int DeductionNumerator { get; set; } = 0;

        [JsonPropertyName("deductionDenominator")]
        public int DeductionDenominator { get; set; } = 0;

        [JsonPropertyName("distributionDurationMonth")]
        public decimal DistributionDurationMonth { get; set; } = 0;

        [JsonPropertyName("deductionCode")]
        public int DeductionCode { get; set; } = 0;

        [JsonPropertyName("foreignTradeType")]
        public int ForeignTradeType { get; set; } = -1;

        [JsonPropertyName("stoppageRate")]
        public decimal StoppageRate { get; set; } = 0;

        [JsonPropertyName("stoppageAmount")]
        public decimal StoppageAmount { get; set; } = 0;

        [JsonPropertyName("stoppageAmountTC")]
        public decimal StoppageAmountTC { get; set; } = 0;

        [JsonPropertyName("distDiscountAmount")]
        public decimal DistDiscountAmount { get; set; } = 0;

        [JsonPropertyName("distCostAmount")]
        public decimal DistCostAmount { get; set; } = 0;

        [JsonPropertyName("beforeDeductionTransTotalVatLC")]
        public decimal BeforeDeductionTransTotalVatLC { get; set; } = 0;

        [JsonPropertyName("beforeDeductionTransTotalVatRC")]
        public decimal BeforeDeductionTransTotalVatRC { get; set; } = 0;

        [JsonPropertyName("decreasedTransTotalVatLC")]
        public decimal DecreasedTransTotalVatLC { get; set; } = 0;

        [JsonPropertyName("decreasedTransTotalVatRC")]
        public decimal DecreasedTransTotalVatRC { get; set; } = 0;

        [JsonPropertyName("transDiscountValue")]
        public decimal TransDiscountValue { get; set; } = 0;

        /// <summary>MIKTAR * FIYAT (iskonto öncesi)</summary>
        [JsonPropertyName("applyDiscountTransValue")]
        public decimal ApplyDiscountTransValue { get; set; }

        [JsonPropertyName("applyDiscountTransValueTC")]
        public decimal ApplyDiscountTransValueTC { get; set; } = 0;

        [JsonPropertyName("applyDiscountTransValueRC")]
        public decimal ApplyDiscountTransValueRC { get; set; } = 0;

        [JsonPropertyName("vatAmountChanged")]
        public int VatAmountChanged { get; set; } = 0;

        [JsonPropertyName("unitConversion")]
        public decimal UnitConversion { get; set; } = 1.0m;

        [JsonPropertyName("unitConversion1")]
        public decimal UnitConversion1 { get; set; } = 1.0m;

        [JsonPropertyName("width")]
        public decimal Width { get; set; } = 0;

        [JsonPropertyName("length")]
        public decimal Length { get; set; } = 0;

        [JsonPropertyName("height")]
        public decimal Height { get; set; } = 0;

        [JsonPropertyName("area")]
        public decimal Area { get; set; } = 0;

        [JsonPropertyName("netVolume")]
        public decimal NetVolume { get; set; } = 0;

        [JsonPropertyName("grossVolume")]
        public decimal GrossVolume { get; set; } = 0;

        [JsonPropertyName("netWeight")]
        public decimal NetWeight { get; set; } = 0;

        [JsonPropertyName("grossWeight")]
        public decimal GrossWeight { get; set; } = 0;

        [JsonPropertyName("lastPurchasePriceCheckbox")]
        public bool LastPurchasePriceCheckbox { get; set; } = false;

        [JsonPropertyName("lastPurchasePrice")]
        public decimal LastPurchasePrice { get; set; } = 0;

        [JsonPropertyName("imei1")]
        public string Imei1 { get; set; } = "";

        [JsonPropertyName("imei2")]
        public string Imei2 { get; set; } = "";

        [JsonPropertyName("macNo")]
        public string MacNo { get; set; } = "";

        [JsonPropertyName("techDeviceType")]
        public int TechDeviceType { get; set; } = 0;

        [JsonPropertyName("localityRate")]
        public decimal LocalityRate { get; set; } = 0;

        [JsonPropertyName("gtipCode")]
        public string GtipCode { get; set; } = "";

        [JsonPropertyName("tagNo")]
        public string TagNo { get; set; } = "";

        [JsonPropertyName("analysisDimLines")]
        public List<SalesInvoiceAnalysisDimLine> AnalysisDimLines { get; set; } = new List<SalesInvoiceAnalysisDimLine>();

        [JsonPropertyName("slDetailsTransaction")]
        public List<object> SlDetailsTransaction { get; set; } = new List<object>();

        [JsonPropertyName("medDeviceDetailTransaction")]
        public List<object> MedDeviceDetailTransaction { get; set; } = new List<object>();

        [JsonPropertyName("extensions")]
        public SalesInvoiceExtensions Extensions { get; set; } = new SalesInvoiceExtensions();

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    // =========================================================================
    // analysisDimLines - Analiz Boyut Satırı
    // =========================================================================

    public class SalesInvoiceAnalysisDimLine
    {
        [JsonPropertyName("analysisdimensioncode")]
        public string AnalysisDimensionCode { get; set; } = "";

        [JsonPropertyName("analysisdimensionDescription")]
        public string AnalysisDimensionDescription { get; set; } = "";

        [JsonPropertyName("projectcode")]
        public string ProjectCode { get; set; } = "";

        [JsonPropertyName("projectDescription")]
        public string ProjectDescription { get; set; } = "";

        [JsonPropertyName("projectactivitycode")]
        public string ProjectActivityCode { get; set; } = "";

        [JsonPropertyName("projectactivityDescription")]
        public string ProjectActivityDescription { get; set; } = "";

        [JsonPropertyName("distributionrate")]
        public decimal DistributionRate { get; set; } = 0;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; } = 0;

        [JsonPropertyName("amountRC")]
        public decimal AmountRC { get; set; } = 0;

        [JsonPropertyName("amountTC")]
        public decimal AmountTC { get; set; } = 0;

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    // =========================================================================
    // extensions - Eklenti nesnesi
    // =========================================================================

    public class SalesInvoiceExtensions
    {
        [JsonPropertyName("list")]
        public List<object> List { get; set; } = new List<object>();
    }

    // =========================================================================
    // masterDataDispatcDTO - İrsaliye Başlık Referansı
    // =========================================================================

    /// <summary>
    /// İrsaliye başlık bilgisi. IRSALIYE_NO bazında distinct olarak oluşturulur.
    /// </summary>
    public class SalesInvoiceMasterDataDispatch
    {
        /// <summary>Kaynak: SLIPTYPE (default 8)</summary>
        [JsonPropertyName("type")]
        public int Type { get; set; } = 8;

        /// <summary>Kaynak: IRSALIYE_NO</summary>
        [JsonPropertyName("number")]
        public string Number { get; set; }

        /// <summary>
        /// Kaynak: IRSALIYE_TARIHI
        /// Logo format: "2026-02-20T10:50:47.000+03:00"
        /// </summary>
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("documenDate")]
        public string DocumenDate { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    // =========================================================================
    // installmentDTO - Ödeme Taksiti
    // =========================================================================

    /// <summary>
    /// Ödeme taksit bilgisi.
    /// amount = SUM(TOTAL) * (1 + VATRATE/100) → KDV dahil toplam
    /// Örn: 1950 * 1.20 = 2340
    /// </summary>
    public class SalesInvoiceInstallment
    {
        [JsonPropertyName("paymentNo")]
        public string PaymentNo { get; set; } = "1";

        /// <summary>Kaynak: ODEME_TARIHI (Logo format)</summary>
        [JsonPropertyName("date")]
        public string Date { get; set; }

        /// <summary>Kaynak: ODEME_TARIHI (Logo format)</summary>
        [JsonPropertyName("optionDate")]
        public string OptionDate { get; set; }

        /// <summary>Ödeme gününe kalan gün sayısı</summary>
        [JsonPropertyName("day")]
        public decimal Day { get; set; } = 0;

        [JsonPropertyName("ppiPercent")]
        public decimal PpiPercent { get; set; } = 0;

        [JsonPropertyName("iodPercent")]
        public decimal IodPercent { get; set; } = 0;

        /// <summary>
        /// KDV dahil toplam tutar.
        /// Hesaplama: SUM(MIKTAR*FIYAT) * (1 + VATRATE/100)
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "1";

        [JsonPropertyName("operationStatus")]
        public int OperationStatus { get; set; } = 0;

        [JsonPropertyName("paymentType")]
        public int PaymentType { get; set; } = 0;

        [JsonPropertyName("discountPercent")]
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>Kaynak: ODEME_TARIHI (Logo format)</summary>
        [JsonPropertyName("discountValidation")]
        public string DiscountValidation { get; set; }

        [JsonPropertyName("amountForDelay")]
        public decimal AmountForDelay { get; set; } = 0;

        [JsonPropertyName("amountAccount")]
        public int AmountAccount { get; set; } = 0;

        [JsonPropertyName("transactionCurrency")]
        public int TransactionCurrency { get; set; } = 0;

        [JsonPropertyName("tcExchangeRate")]
        public decimal TcExchangeRate { get; set; } = 0;

        [JsonPropertyName("amountTC")]
        public decimal AmountTC { get; set; } = 0;

        [JsonPropertyName("serviceCommission")]
        public decimal ServiceCommission { get; set; } = 0;

        [JsonPropertyName("pointCommission")]
        public decimal PointCommission { get; set; } = 0;

        [JsonPropertyName("lateChargeCommission")]
        public decimal LateChargeCommission { get; set; } = 0;

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    // =========================================================================
    // Time modeli (sipariş modülündeki TimeDto ile aynı yapı)
    // =========================================================================

    /// <summary>
    /// Saat bilgisi
    /// </summary>
    public class SalesInvoiceTime
    {
        [JsonPropertyName("hour")]
        public int Hour { get; set; }

        [JsonPropertyName("minute")]
        public int Minute { get; set; }

        [JsonPropertyName("second")]
        public int Second { get; set; } = 0;

        [JsonPropertyName("milisecond")]
        public int Milisecond { get; set; } = 0;
    }
}
