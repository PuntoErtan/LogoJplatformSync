// =============================================
// LogoSync.Core/DTOs/PuntoCashReceiptDto.cs
// =============================================

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// PUNTO.ERYAZ_TAHSILAT tablosundan gelen veri modeli
    /// </summary>
    public class PuntoCashReceiptDto
    {
        public int Id { get; set; }                  // ID
        public int No { get; set; }                  // NO → ReceiptNo
        public string CariKodu { get; set; }         // CARI_KODU → CustomerCode
        public string CariUnvani { get; set; }       // CARI_UNVANI → CustomerName
        public DateTime? Tarih { get; set; }         // TARIH → ReceiptDate
        public decimal Tutar { get; set; }           // TUTAR → Amount
        public string Not { get; set; }              // NOT → Description
        public int? TahsilatTipi { get; set; }       // TAHSILAT_TIPI (0=Nakit, 1=Çek)
        public string PlasiyerKodu { get; set; }     // PLASIYER_KODU (opsiyonel)

        // Çek/Senet alanları
        public string BankaAdi { get; set; }         // BANKA_ADI → BankName
        public string CekSenetNo { get; set; }       // CEK_SENET_NO → ChequeNo
        public DateTime? VadeTarihi { get; set; }    // VADE_TARIHI → DueDate
    }
}
