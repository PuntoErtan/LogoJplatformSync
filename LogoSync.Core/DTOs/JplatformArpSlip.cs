// =============================================
// LogoSync.Core/DTOs/JplatformArpSlip.cs
// Logo J-Platform API: POST /arpslips?slipType=08
// Cari Hesap Fişi (Sanal POS)
// =============================================

using System.Text.Json.Serialization;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// Logo J-Platform Cari Hesap Fişi (ARP Slip) - slipType=08
    /// </summary>
    public class JplatformArpSlip
    {
        [JsonPropertyName("slipDate")]
        public string SlipDate { get; set; }

        [JsonPropertyName("slipNo")]
        public string SlipNo { get; set; }

        [JsonPropertyName("docNo")]
        public string DocNo { get; set; } 

        [JsonPropertyName("orgUnitCode")]
        public string OrgUnitCode { get; set; }

        [JsonPropertyName("footnote")]
        public string footnote { get; set; }

        [JsonPropertyName("slipTransaction")]
        public List<ArpSlipTransaction> SlipTransaction { get; set; } = new();

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }

    public class ArpSlipTransaction
    {
        [JsonPropertyName("arpInfoCode")]
        public string ArpInfoCode { get; set; }

        [JsonPropertyName("arpInfoDef")]
        public string ArpInfoDef { get; set; }

        [JsonPropertyName("exchRateDifferenceCurrencyType")]
        public int ExchRateDifferenceCurrencyType { get; set; } = -1;

        [JsonPropertyName("lineCredit")]
        public decimal LineCredit { get; set; }

        [JsonPropertyName("reportingCurrency")]
        public string ReportingCurrency { get; set; } = "USD";

        [JsonPropertyName("paymentPlanCode")]
        public string PaymentPlanCode { get; set; }

        [JsonPropertyName("transType")]
        public int TransType { get; set; } = -1;

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; }

        [JsonPropertyName("crossAccType")]
        public int CrossAccType { get; set; } = 2;

        [JsonPropertyName("crossAccCode")]
        public string CrossAccCode { get; set; }

        [JsonPropertyName("crossAccDef")]
        public string CrossAccDef { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("serializeNulls")]
        public bool SerializeNulls { get; set; } = false;
    }
}
