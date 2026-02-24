namespace LogoSync.Core.DTOs
{
    public class CashReceiptDto
    {
        public long SourceId { get; set; }
        public string ReceiptNo { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CashAccountCode { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Description { get; set; }

        // Yeni alanlar - Çek desteği
        public int ReceiptType { get; set; }      // 0=Nakit, 1=Çek
        public string BankName { get; set; }       // Banka adı
        public string ChequeNo { get; set; }       // Çek/Senet numarası
        public DateTime? DueDate { get; set; }     // Vade tarihi
    }

    public static class ReceiptTypes
    {
        public const int Nakit = 0;
        public const int Cek = 1;
    }
}
