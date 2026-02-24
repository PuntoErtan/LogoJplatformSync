namespace LogoSync.Core.DTOs
{
    public class JplatformApiResponse
    {
        public bool Success { get; set; }
        public int? LogicalRef { get; set; }
        public string TransactionNo { get; set; }
        public string Code { get; set; }              // Response "code" alanı (ör: "SIL-202600000001")
        public string Data { get; set; }
        public int? ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}