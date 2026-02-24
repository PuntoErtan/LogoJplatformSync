using System;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// PUNTO.dbo.PNTV_005_IrsaliyeDetay_Faturala view'ından okunan satır.
    /// Her satır bir irsaliye detay kaydını temsil eder.
    /// CARI_KOD bazında gruplanarak tek fatura oluşturulur.
    /// </summary>
    public class SalesInvoiceDetailDto
    {
        // ── İrsaliye Bilgileri ─────────────────────────────────────
        public string IrsaliyeNo { get; set; }
        public long IrsaliyeRef { get; set; }
        public DateTime IrsaliyeTarihi { get; set; }
        public long IrsaliyeSatirRef { get; set; }

        // ── Sipariş Bilgileri ──────────────────────────────────────
        public string SiparisNo { get; set; }
        public DateTime? SiparisTarihi { get; set; }
        public long SiparisRef { get; set; }
        public long SiparisSatirRef { get; set; }

        // ── Miktar / Fiyat ─────────────────────────────────────────
        public decimal Miktar { get; set; }
        public decimal Fiyat { get; set; }
        public int FiyatKur { get; set; }

        // ── Cari Bilgileri ─────────────────────────────────────────
        public string CariKod { get; set; }
        public string Cari { get; set; }

        // ── Ürün Bilgileri ─────────────────────────────────────────
        public string UrunKodu { get; set; }
        public string Urun { get; set; }

        // ── Ödeme Bilgileri ────────────────────────────────────────
        public long OdemePlaniRef { get; set; }
        public int OdemeGunu { get; set; }
        public DateTime? OdemeTarihi { get; set; }
        public string OdemePlani { get; set; }
        public string SatirOdemePlani { get; set; }

        // ── Organizasyon ───────────────────────────────────────────
        public string Isyeri { get; set; }
        public string Ambar { get; set; }
        public string Birim { get; set; }
        public string SatisElemani { get; set; }

        // ── Durum & Tip ────────────────────────────────────────────
        public int BoStatus { get; set; }
        public int BillStatus { get; set; }
        public int LineType { get; set; }       // 0=Malzeme, 2=İskonto
        public int SlipType { get; set; }       // default 8

        // ── Logo Referanslar ───────────────────────────────────────
        public long ContTransRef { get; set; }
        public long ParentLineRef { get; set; }
        public int DetailLineNr { get; set; }
        public int DetLine { get; set; }
        public int MmSlipLnNr { get; set; }
        public long WorkOrderRef { get; set; }
        public long WoAssetRef { get; set; }
        public int IoCategory { get; set; }     // default 4
        public long InvoiceRef { get; set; }    // faturalama sonrası atanır
        public int InvoiceLnNr { get; set; }
        public long ItemRef { get; set; }
        public long ArpRef { get; set; }
        public long OrdTransRef { get; set; }
        public long OrdSlipRef { get; set; }
        public int GlobTrans { get; set; }      // default 0
        public int CalcType { get; set; }       // default 0
        public long PrdOrderRef { get; set; }   // default 0
        public long PromotionRef { get; set; }  // default 0

        // ── Tutar Bilgileri ────────────────────────────────────────
        public decimal Total { get; set; }      // MIKTAR * FIYAT
        public int PcType { get; set; }
        public decimal PcPrice { get; set; }
        public decimal PcRate { get; set; }
        public int TcType { get; set; }
        public decimal TcRate { get; set; }
        public decimal RcRate { get; set; }
        public decimal DistCost { get; set; }
        public decimal DistDisc { get; set; }
        public decimal DistExp { get; set; }
        public decimal DistProm { get; set; }
        public decimal DiscPer { get; set; }    // İskonto yüzdesi (LINETYPE=2 için)
        public string LineExp { get; set; }

        // ── Birim Bilgileri ────────────────────────────────────────
        public long UomRef { get; set; }
        public long UomSetRef { get; set; }
        public decimal UInfo1 { get; set; }     // default 1
        public decimal UInfo2 { get; set; }     // default 1

        // ── KDV Bilgileri ──────────────────────────────────────────
        public int VatInc { get; set; }         // default 0
        public decimal VatRate { get; set; }    // KDV oranı (20 vb.)
        public decimal VatAmnt { get; set; }
        public decimal VatMatrah { get; set; }

        // ── Faturalama Durumu ──────────────────────────────────────
        public int BilledItem { get; set; }
        public int Billed { get; set; }         // faturalama sonrası 1 olacak
        public int TrAssetType { get; set; }

        // ── Hesaplanan ─────────────────────────────────────────────
        public decimal LineNet { get; set; }    // installmentDTO.amount hesabı için
    }
}
