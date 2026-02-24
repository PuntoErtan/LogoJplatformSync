// =============================================
// LogoSync.Core/DTOs/SanalPosReceiptDto.cs
// PUNTO View: PNTV_ERYAZ_SANALPOS_AKTILACAKLAR
// =============================================

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// PUNTO PNTV_ERYAZ_SANALPOS_AKTILACAKLAR view'ından gelen sanal POS kaydı
    /// </summary>
    public class SanalPosReceiptDto
    {
        public int Id { get; set; }
        public string Nereden { get; set; }
        public string BelgeNo { get; set; }
        public string Tur { get; set; }
        public DateTime Tarih { get; set; }
        public string CariKodu { get; set; }
        public string CariUnvani { get; set; }
        public string FisNo { get; set; }
        public decimal Tutar { get; set; }
        public string PlasiyerKodu { get; set; }
        public string Organizasyon { get; set; }
        public string BankaHesapKodu { get; set; }
        public string BankaHesapAciklama { get; set; }
        public string OdemePlaniKodu { get; set; }
        public int Aktarim { get; set; }
    }
}
