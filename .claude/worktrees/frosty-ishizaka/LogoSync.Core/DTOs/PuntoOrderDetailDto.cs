using System;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// PUNTO ERYAZ_SIPARIS_DETAY tablosundan okunan sipariş satır verisi
    /// </summary>
    public class PuntoOrderDetailDto
    {
        public int Id { get; set; }
        public string SiparisNo { get; set; }
        public string UrunKodu { get; set; }
        public string UrunAdi { get; set; }
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal? Isk1 { get; set; }
        public decimal? Isk2 { get; set; }
        public decimal? Isk3 { get; set; }
        public decimal? Isk4 { get; set; }
        public decimal? IskKampanya { get; set; }
        public decimal? IskCep { get; set; }
        public int? Vade { get; set; }
        public string Birim { get; set; }
        public string FisNo { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public decimal? PesinIsk { get; set; }
    }
}
