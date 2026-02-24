using System.Text.Json.Serialization;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// Logo REST Services v2.0 - Kasa Fişi
    /// POST /logo/restservices/rest/v2.0/safedepositslips
    /// GET response'undan alınan tam model
    /// </summary>
    public class JplatformSafeDepositSlip
    {
        [JsonPropertyName("transactionNo")]
        public string TransactionNo { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("hour")]
        public HourModel Hour { get; set; }

        [JsonPropertyName("pCPointCode")]
        public string PCPointCode { get; set; }

        [JsonPropertyName("pCPointDescription")]
        public string PCPointDescription { get; set; }

        [JsonPropertyName("safeDepositCode")]
        public string SafeDepositCode { get; set; }

        [JsonPropertyName("safeDepositDescription")]
        public string SafeDepositDescription { get; set; }

        [JsonPropertyName("safeDepositBalance")]
        public decimal? SafeDepositBalance { get; set; }

        [JsonPropertyName("transaction")]
        public List<object> Transaction { get; set; }

        [JsonPropertyName("transaction2")]
        public List<SafeDepositTransaction> Transaction2 { get; set; }

        [JsonPropertyName("transaction3")]
        public List<object> Transaction3 { get; set; }

        [JsonPropertyName("transaction4")]
        public List<object> Transaction4 { get; set; }

        [JsonPropertyName("openingGLSlipType")]
        public string OpeningGLSlipType { get; set; }

        [JsonPropertyName("openingGLSlipDescription")]
        public string OpeningGLSlipDescription { get; set; }

        [JsonPropertyName("journalGLSlipType")]
        public string JournalGLSlipType { get; set; }

        [JsonPropertyName("journalGLSlipDescription")]
        public string JournalGLSlipDescription { get; set; }

        [JsonPropertyName("collectionGLSlipType")]
        public string CollectionGLSlipType { get; set; }

        [JsonPropertyName("collectionGLSlipDescription")]
        public string CollectionGLSlipDescription { get; set; }

        [JsonPropertyName("paymentGLSlipType")]
        public string PaymentGLSlipType { get; set; }

        [JsonPropertyName("paymentGLSlipDescription")]
        public string PaymentGLSlipDescription { get; set; }

        [JsonPropertyName("debit")]
        public decimal? Debit { get; set; }

        [JsonPropertyName("debitRC")]
        public decimal? DebitRC { get; set; }

        [JsonPropertyName("numberOfRecords")]
        public int? NumberOfRecords { get; set; }

        [JsonPropertyName("debit2")]
        public decimal? Debit2 { get; set; }

        [JsonPropertyName("debitRC2")]
        public decimal? DebitRC2 { get; set; }

        [JsonPropertyName("numberOfRecords2")]
        public int? NumberOfRecords2 { get; set; }

        [JsonPropertyName("debit3")]
        public decimal? Debit3 { get; set; }

        [JsonPropertyName("debitRC3")]
        public decimal? DebitRC3 { get; set; }

        [JsonPropertyName("numberOfRecords3")]
        public int? NumberOfRecords3 { get; set; }

        [JsonPropertyName("extensions")]
        public ExtensionsModel Extensions { get; set; }

        [JsonPropertyName("index")]
        public int? Index { get; set; }
    }

    public class HourModel
    {
        [JsonPropertyName("hour")]
        public int Hour { get; set; }

        [JsonPropertyName("minute")]
        public int Minute { get; set; }

        [JsonPropertyName("second")]
        public int Second { get; set; }

        [JsonPropertyName("milisecond")]
        public int Milisecond { get; set; }
    }

    public class ExtensionsModel
    {
        [JsonPropertyName("list")]
        public List<object> List { get; set; } = new();
    }

    public class SafeDepositTransaction
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("transactionLRef")]
        public int? TransactionLRef { get; set; }

        [JsonPropertyName("transactionNumber")]
        public string TransactionNumber { get; set; }

        [JsonPropertyName("documentNo")]
        public string DocumentNo { get; set; }

        [JsonPropertyName("documentDate")]
        public string DocumentDate { get; set; }

        [JsonPropertyName("auxCode")]
        public string AuxCode { get; set; }

        [JsonPropertyName("authCode")]
        public string AuthCode { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("aRaPBranches")]
        public string ARaPBranches { get; set; }

        [JsonPropertyName("aRaPBranchesDescription")]
        public string ARaPBranchesDescription { get; set; }

        [JsonPropertyName("purchaseEmployeeorSalespersonCode")]
        public string PurchaseEmployeeorSalespersonCode { get; set; }

        [JsonPropertyName("aRaPCode")]
        public string ARaPCode { get; set; }

        [JsonPropertyName("aRaPTitle")]
        public string ARaPTitle { get; set; }

        [JsonPropertyName("tradingGroup")]
        public string TradingGroup { get; set; }

        [JsonPropertyName("comboboxReturn")]
        public int? ComboboxReturn { get; set; }

        [JsonPropertyName("contractNumber")]
        public string ContractNumber { get; set; }

        [JsonPropertyName("importExportFileCode")]
        public string ImportExportFileCode { get; set; }

        [JsonPropertyName("bankAccountCode")]
        public string BankAccountCode { get; set; }

        [JsonPropertyName("bankAccountName")]
        public string BankAccountName { get; set; }

        [JsonPropertyName("pCPointCode")]
        public string PCPointCode { get; set; }

        [JsonPropertyName("pCPointDescription")]
        public string PCPointDescription { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("rcAmount")]
        public decimal? RcAmount { get; set; }

        [JsonPropertyName("rcExchangeRate")]
        public decimal? RcExchangeRate { get; set; }

        [JsonPropertyName("tcType")]
        public int TcType { get; set; }

        [JsonPropertyName("amountTC")]
        public decimal AmountTC { get; set; }

        [JsonPropertyName("tcExchangeRate")]
        public decimal TcExchangeRate { get; set; }

        [JsonPropertyName("tAccount")]
        public int? TAccount { get; set; }

        [JsonPropertyName("tDistributionRate")]
        public int? TDistributionRate { get; set; }

        [JsonPropertyName("tAmount")]
        public decimal? TAmount { get; set; }

        [JsonPropertyName("tAmountRC")]
        public decimal? TAmountRC { get; set; }

        [JsonPropertyName("tAmountTC")]
        public decimal? TAmountTC { get; set; }

        [JsonPropertyName("missingLC")]
        public decimal? MissingLC { get; set; }

        [JsonPropertyName("missingRC")]
        public decimal? MissingRC { get; set; }

        [JsonPropertyName("missingFC")]
        public decimal? MissingFC { get; set; }

        [JsonPropertyName("totalLC")]
        public decimal? TotalLC { get; set; }

        [JsonPropertyName("totalRC")]
        public decimal? TotalRC { get; set; }

        [JsonPropertyName("totalFC")]
        public decimal? TotalFC { get; set; }

        [JsonPropertyName("rateMissingAmount")]
        public int? RateMissingAmount { get; set; }

        [JsonPropertyName("payrollPayment")]
        public string PayrollPayment { get; set; }

        [JsonPropertyName("payrollPaymentDescription")]
        public string PayrollPaymentDescription { get; set; }

        [JsonPropertyName("gLAccountCode")]
        public string GLAccountCode { get; set; }

        [JsonPropertyName("gLAccountName")]
        public string GLAccountName { get; set; }

        [JsonPropertyName("registryNumber")]
        public string RegistryNumber { get; set; }

        [JsonPropertyName("name2")]
        public string Name2 { get; set; }

        [JsonPropertyName("surname2")]
        public string Surname2 { get; set; }

        [JsonPropertyName("orgUnit")]
        public string OrgUnit { get; set; }

        [JsonPropertyName("operationCategory")]
        public int? OperationCategory { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; }

        [JsonPropertyName("serviceType")]
        public int? ServiceType { get; set; }

        [JsonPropertyName("customerSurname")]
        public string CustomerSurname { get; set; }

        [JsonPropertyName("stoppageRate")]
        public int? StoppageRate { get; set; }

        [JsonPropertyName("stoppageRateAmount")]
        public decimal? StoppageRateAmount { get; set; }

        [JsonPropertyName("registrationNumber")]
        public string RegistrationNumber { get; set; }

        [JsonPropertyName("fundShareRate")]
        public int? FundShareRate { get; set; }

        [JsonPropertyName("fundShareRateAmount")]
        public decimal? FundShareRateAmount { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }

        [JsonPropertyName("trIdentificationNoorTaxNo")]
        public string TrIdentificationNoorTaxNo { get; set; }

        [JsonPropertyName("privateCompany")]
        public bool? PrivateCompany { get; set; }

        [JsonPropertyName("stoppageRate2")]
        public int? StoppageRate2 { get; set; }

        [JsonPropertyName("stoppageRateAmount2")]
        public decimal? StoppageRateAmount2 { get; set; }

        [JsonPropertyName("deductionwillbeapplied")]
        public int? DeductionWillBeApplied { get; set; }

        [JsonPropertyName("fundShareRate2")]
        public int? FundShareRate2 { get; set; }

        [JsonPropertyName("fundShareRateAmount2")]
        public decimal? FundShareRateAmount2 { get; set; }

        [JsonPropertyName("deductionwillbedistributed")]
        public int? DeductionWillBeDistributed { get; set; }

        [JsonPropertyName("vatongross")]
        public int? VatOnGross { get; set; }

        [JsonPropertyName("vatRateAmount")]
        public decimal? VatRateAmount { get; set; }

        [JsonPropertyName("deductionRate")]
        public int? DeductionRate { get; set; }

        [JsonPropertyName("deductionRate2")]
        public int? DeductionRate2 { get; set; }

        [JsonPropertyName("address2")]
        public string Address2 { get; set; }

        [JsonPropertyName("deductionsTotal")]
        public decimal? DeductionsTotal { get; set; }

        [JsonPropertyName("grossFee")]
        public decimal? GrossFee { get; set; }

        [JsonPropertyName("netFee")]
        public decimal? NetFee { get; set; }

        [JsonPropertyName("vatIncluded")]
        public bool? VatIncluded { get; set; }

        [JsonPropertyName("vatRate")]
        public decimal? VatRate { get; set; }

        [JsonPropertyName("vatAmount")]
        public decimal? VatAmount { get; set; }

        [JsonPropertyName("vatInclusiveAmount")]
        public decimal? VatInclusiveAmount { get; set; }

        [JsonPropertyName("noteTransaction")]
        public List<object> NoteTransaction { get; set; }

        [JsonPropertyName("totalLocalCurrency")]
        public decimal? TotalLocalCurrency { get; set; }

        [JsonPropertyName("averageDay")]
        public decimal? AverageDay { get; set; }

        [JsonPropertyName("numberofRecords")]
        public decimal? NumberOfRecords { get; set; }

        [JsonPropertyName("remainingAmountInLC")]
        public decimal? RemainingAmountInLC { get; set; }

        [JsonPropertyName("remainingAmountInRC")]
        public decimal? RemainingAmountInRC { get; set; }

        [JsonPropertyName("remainingAmountInFC")]
        public decimal? RemainingAmountInFC { get; set; }

        [JsonPropertyName("totalInLC")]
        public decimal? TotalInLC { get; set; }

        [JsonPropertyName("totalInRC")]
        public decimal? TotalInRC { get; set; }

        [JsonPropertyName("totalInFC")]
        public decimal? TotalInFC { get; set; }

        [JsonPropertyName("remainingRate")]
        public decimal? RemainingRate { get; set; }

        [JsonPropertyName("index")]
        public int? Index { get; set; }
    }

    public static class TransactionTypes
    {
        public const int NakitTahsilat = 11;
        public const int NakitOdeme = 12;
    }
}