using System.Text.Json.Serialization;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// Logo REST Services v2.0 - Çek Giriş Bordrosu
    /// POST /logo/restservices/rest/v2.0/chequepnoteslips?slipType=1
    /// </summary>
    public class JplatformChequePNoteSlip
    {
        [JsonPropertyName("slipDate")]
        public string SlipDate { get; set; }

        [JsonPropertyName("orgUnit")]
        public string OrgUnit { get; set; }

        [JsonPropertyName("orgUnitDescription")]
        public string OrgUnitDescription { get; set; }

        [JsonPropertyName("arapCode")]
        public string ArapCode { get; set; }

        [JsonPropertyName("arapTitle")]
        public string ArapTitle { get; set; }

        [JsonPropertyName("SalespersonCode")]
        public string SalespersonCode { get; set; }

        [JsonPropertyName("noteTransaction")]
        public List<ChequeNoteTransaction> NoteTransaction { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("totalLocalCurrency")]
        public decimal TotalLocalCurrency { get; set; }

        [JsonPropertyName("avergDueDate")]
        public string AvergDueDate { get; set; }

        [JsonPropertyName("numberofRecords")]
        public decimal NumberOfRecords { get; set; }

        [JsonPropertyName("bankTransaction")]
        public List<BankTransactionItem> BankTransaction { get; set; }

        [JsonPropertyName("analysisTransaction")]
        public List<AnalysisTransactionItem> AnalysisTransaction { get; set; }

        [JsonPropertyName("remainingRate")]
        public decimal RemainingRate { get; set; }

        [JsonPropertyName("mainChartofAccounts")]
        public string MainChartOfAccounts { get; set; }

        [JsonPropertyName("mainChartofAccountsDesc")]
        public string MainChartOfAccountsDesc { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class ChequeNoteTransaction
    {
        [JsonPropertyName("portfolioNo")]
        public string PortfolioNo { get; set; }

        [JsonPropertyName("serialNo")]
        public string SerialNo { get; set; }

        [JsonPropertyName("dueDate")]
        public string DueDate { get; set; }

        [JsonPropertyName("debtor")]
        public string Debtor { get; set; }

        [JsonPropertyName("placeOfPayment")]
        public string PlaceOfPayment { get; set; }

        [JsonPropertyName("bankName")]
        public string BankName { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("chequePromNoteTransaction")]
        public List<object> ChequePromNoteTransaction { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class BankTransactionItem
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class AnalysisTransactionItem
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
