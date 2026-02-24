using System;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// PUNTO ERYAZ_SIPARISLER tablosundan okunan sipariş başlık verisi
    /// </summary>
    public class PuntoOrderDto
    {
        public int Id { get; set; }
        public string No { get; set; }
        public string CariKodu { get; set; }
        public string CariUnvani { get; set; }
        public string BelgeNo { get; set; }
        public string GonderiSekli { get; set; }
        public DateTime SiparisTarihi { get; set; }
        public string OzelKod { get; set; }
        public string TemsilciKodu { get; set; }
        public string Gonderen { get; set; }
        public string FisNo { get; set; }
        public string Depo { get; set; }
        public string SiparisNotu { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public int? Aktarim { get; set; }
        public decimal? Isk1 { get; set; }
        public decimal? Isk2 { get; set; }
        public decimal? Isk3 { get; set; }
        public string DbName { get; set; }
        public int? Firma { get; set; }
        public int? Donem { get; set; }
        public int? Perakende { get; set; }
    }
}
