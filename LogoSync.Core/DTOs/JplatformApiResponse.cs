namespace LogoSync.Core.DTOs
{
    public class JplatformApiResponse
    {
        public bool Success { get; set; }
        public int? LogicalRef { get; set; }
        public string TransactionNo { get; set; }
        public string Code { get; set; }              // Response "code" alanı (ör: "ODO2025000000001")
        public string Data { get; set; }
        public int? ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Fatura numarasının seri kodu (ilk 3 karakter).
        /// Örn: "ODO2025000000001" → "ODO"
        /// </summary>
        public string InvoiceNoPart1 =>
            !string.IsNullOrEmpty(Code) && Code.Length >= 3 ? Code.Substring(0, 3) : null;

        /// <summary>
        /// Fatura numarasının sıra numarası (3. karakterden sonrası).
        /// Örn: "ODO2025000000001" → "2025000000001"
        /// </summary>
        public string InvoiceNoPart2 =>
            !string.IsNullOrEmpty(Code) && Code.Length > 3 ? Code.Substring(3) : null;
    }
}