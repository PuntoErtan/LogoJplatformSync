using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// Logo J-Platform REST API /salesOrder endpoint request modeli
    /// </summary>
    public class JplatformOrderSlip
    {
        [JsonPropertyName("orderDiscountDTO")]
        public List<OrderLineDto> OrderDiscountDTO { get; set; } = new List<OrderLineDto>();

        [JsonPropertyName("no")]
        public string No { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("time")]
        public TimeDto Time { get; set; }

        [JsonPropertyName("documentNo")]
        public string DocumentNo { get; set; }

        [JsonPropertyName("documentDate")]
        public string DocumentDate { get; set; }

        [JsonPropertyName("orgUnit")]
        public string OrgUnit { get; set; }

        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; }

        [JsonPropertyName("arap")]
        public string Arap { get; set; }

        [JsonPropertyName("salespersonCode")]
        public string SalespersonCode { get; set; }

        [JsonPropertyName("paymentPlan")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PaymentPlan { get; set; }

        [JsonPropertyName("code4")]
        public string Code4 { get; set; }

        [JsonPropertyName("tradingGroup")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TradingGroup { get; set; }

        [JsonPropertyName("documentTracking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string DocumentTracking { get; set; }

        [JsonPropertyName("prePayment")]
        public bool PrePayment { get; set; } = false;

        [JsonPropertyName("shipmentAddressasInvoiceAddress")]
        public bool ShipmentAddressAsInvoiceAddress { get; set; } = false;

        [JsonPropertyName("reverseChargeApplicability")]
        public int ReverseChargeApplicability { get; set; } = -1;

        [JsonPropertyName("shipmentAddressasInvoiceAddress2")]
        public bool ShipmentAddressAsInvoiceAddress2 { get; set; } = false;

        [JsonPropertyName("einvoice")]
        public bool Einvoice { get; set; } = false;

        [JsonPropertyName("earchive")]
        public bool Earchive { get; set; } = false;

        [JsonPropertyName("onlineSalesInvoice")]
        public bool OnlineSalesInvoice { get; set; } = false;

        [JsonPropertyName("installationNumber")]
        public string InstallationNumber { get; set; } = "";

        [JsonPropertyName("sendingDate")]
        public string SendingDate { get; set; }

        [JsonPropertyName("refreshOrderNumber")]
        public bool RefreshOrderNumber { get; set; } = false;

        [JsonPropertyName("campaignRefs")]
        public List<object> CampaignRefs { get; set; } = new List<object>();

        [JsonPropertyName("extensions")]
        public ExtensionsDto Extensions { get; set; } = new ExtensionsDto();

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    /// <summary>
    /// Sipariş satırı (type=0: Malzeme, type=2: İskonto)
    /// </summary>
    public class OrderLineDto
    {
        [JsonPropertyName("orderAnalysisDTO")]
        public List<OrderAnalysisDto> OrderAnalysisDTO { get; set; } = new List<OrderAnalysisDto>();

        [JsonPropertyName("deep")]
        public bool Deep { get; set; } = false;

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("undeliveredQuantity")]
        public decimal UndeliveredQuantity { get; set; }

        [JsonPropertyName("unit")]
        public int Unit { get; set; }

        [JsonPropertyName("unitCode")]
        public string UnitCode { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("currencyTypeRC")]
        public int CurrencyTypeRC { get; set; } = 1;

        [JsonPropertyName("percent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal Percent { get; set; }

        [JsonPropertyName("vatratePercent")]
        public decimal VatratePercent { get; set; }

        [JsonPropertyName("vatincluded")]
        public bool VatIncluded { get; set; } = false;

        [JsonPropertyName("vatamount")]
        public decimal VatAmount { get; set; }

        [JsonPropertyName("netDiscount")]
        public bool NetDiscount { get; set; } = false;

        [JsonPropertyName("gstincluded")]
        public bool GstIncluded { get; set; } = false;

        [JsonPropertyName("igstrate")]
        public decimal IgstRate { get; set; }

        [JsonPropertyName("igstamount")]
        public decimal IgstAmount { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonPropertyName("additionalTaxIncluded")]
        public bool AdditionalTaxIncluded { get; set; } = false;

        [JsonPropertyName("procurementDate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ProcurementDate { get; set; }

        [JsonPropertyName("reserved")]
        public bool Reserved { get; set; } = false;

        [JsonPropertyName("status")]
        public int Status { get; set; } = 1;

        [JsonPropertyName("purchaseEmployeeSalespersonCode")]
        public string PurchaseEmployeeSalespersonCode { get; set; }

        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; }

        [JsonPropertyName("subjecttoInspection")]
        public bool SubjectToInspection { get; set; } = false;

        [JsonPropertyName("medDeviceDetailsTransactions")]
        public List<object> MedDeviceDetailsTransactions { get; set; } = new List<object>();

        [JsonPropertyName("appliedCampaings")]
        public List<object> AppliedCampaings { get; set; } = new List<object>();

        [JsonPropertyName("extensions")]
        public ExtensionsDto Extensions { get; set; } = new ExtensionsDto();

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    /// <summary>
    /// Sipariş satır analiz boyutları
    /// </summary>
    public class OrderAnalysisDto
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
        public decimal DistributionRate { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("amountRC")]
        public decimal AmountRC { get; set; }

        [JsonPropertyName("amountTC")]
        public decimal AmountTC { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    public class TimeDto
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

    public class ExtensionsDto
    {
        [JsonPropertyName("list")]
        public List<object> List { get; set; } = new List<object>();
    }
}
