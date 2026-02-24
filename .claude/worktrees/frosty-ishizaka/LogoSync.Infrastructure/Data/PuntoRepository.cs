// =============================================
// LogoSync.Infrastructure/Data/PuntoRepository.cs
// =============================================

using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using LogoSync.Core.DTOs;
using LogoSync.Core.Interfaces;

namespace LogoSync.Infrastructure.Data
{
    public class PuntoRepository : IPuntoRepository
    {
        private readonly string _connectionString;

        public PuntoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Tahsilat Methods (Nakit + Çek)

        /// <summary>
        /// PUNTO'dan aktarılmamış tahsilatları getirir (Nakit + Çek)
        /// TAHSILAT_TIPI: 0=Nakit, 1=Çek
        /// </summary>
        public async Task<IEnumerable<PuntoCashReceiptDto>> GetPendingCashReceiptsAsync(int batchSize)
        {
            var items = new List<PuntoCashReceiptDto>();

            const string sql = @"
                SELECT TOP (@BatchSize)
                    [ID],
                    [NO],
                    [CARI_KODU],
                    [CARI_UNVANI],
                    [TARIH],
                    [TUTAR],
                    [NOT],
                    [TAHSILAT_TIPI],
                    [PLASIYER_KODU],
                    [BANKA_ADI],
                    [CEK_SENET_NO],
                    [VADE_TARIHI]
                FROM [dbo].[ERYAZ_TAHSILAT]
                WHERE [TAHSILAT_TIPI] IN (0, 1)
                  AND ([AKTARIM] IS NULL OR [AKTARIM] <> 50)
                  AND [TUTAR] > 0
                  AND [TARIH] IS NOT NULL
                  AND [CARI_KODU] IS NOT NULL
                ORDER BY [ID]";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@BatchSize", batchSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new PuntoCashReceiptDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    No = reader.GetInt32(reader.GetOrdinal("NO")),
                    CariKodu = reader.GetString(reader.GetOrdinal("CARI_KODU")),
                    CariUnvani = reader.IsDBNull(reader.GetOrdinal("CARI_UNVANI"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CARI_UNVANI")),
                    Tarih = reader.IsDBNull(reader.GetOrdinal("TARIH"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("TARIH")),
                    Tutar = reader.IsDBNull(reader.GetOrdinal("TUTAR"))
                        ? 0
                        : reader.GetDecimal(reader.GetOrdinal("TUTAR")),
                    Not = reader.IsDBNull(reader.GetOrdinal("NOT"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("NOT")),
                    TahsilatTipi = reader.IsDBNull(reader.GetOrdinal("TAHSILAT_TIPI"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("TAHSILAT_TIPI")),
                    PlasiyerKodu = reader.IsDBNull(reader.GetOrdinal("PLASIYER_KODU"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PLASIYER_KODU")),
                    // Çek alanları
                    BankaAdi = reader.IsDBNull(reader.GetOrdinal("BANKA_ADI"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("BANKA_ADI")),
                    CekSenetNo = reader.IsDBNull(reader.GetOrdinal("CEK_SENET_NO"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CEK_SENET_NO")),
                    VadeTarihi = reader.IsDBNull(reader.GetOrdinal("VADE_TARIHI"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("VADE_TARIHI"))
                });
            }

            return items;
        }

        public async Task MarkAsTransferredAsync(int puntoId, string batchGuid)
        {
            const string sql = @"
                UPDATE [dbo].[ERYAZ_TAHSILAT]
                SET [AKTARIM] = 50,
                    [AKTARIM_TARIHI] = GETDATE(),
                    [AKTARIM_GUID] = @BatchGuid
                WHERE [ID] = @Id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", puntoId);
            command.Parameters.AddWithValue("@BatchGuid", batchGuid);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region Sipariş Methods

        /// <summary>
        /// PUNTO'dan aktarılmamış siparişleri getirir (header)
        /// </summary>
        public async Task<List<PuntoOrderDto>> GetPendingOrdersAsync(int batchSize)
        {
            var orders = new List<PuntoOrderDto>();

            const string sql = @"
                SELECT TOP (@BatchSize)
                    [ID], [NO], [CARI_KODU], [CARI_UNVANI], [BELGE_NO],
                    [GONDERI_SEKLI], [SIPARIS_TARIHI], [OZEL_KOD], [TEMSILCI_KODU],
                    [GONDEREN], [FIS_NO], [DEPO], [SIPARIS_NOTU], [KAYIT_TARIHI],
                    [AKTARIM], [ISK1], [ISK2], [ISK3], [DBNAME], [FIRMA], [DONEM], [PERAKENDE]
                FROM [dbo].[ERYAZ_SIPARISLER]
                WHERE ([AKTARIM] IS NULL)
                  AND [SIPARIS_TARIHI] IS NOT NULL
                  AND [SIPARIS_TARIHI] >= '2026-01-01'
                  AND [CARI_KODU] IS NOT NULL
                  AND [FIS_NO] IS NOT NULL
                ORDER BY [ID]";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@BatchSize", batchSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new PuntoOrderDto
                {
                    Id = Convert.ToInt32(reader["ID"]),
                    No = reader.IsDBNull(reader.GetOrdinal("NO")) ? null : reader["NO"].ToString(),
                    CariKodu = reader["CARI_KODU"].ToString(),
                    CariUnvani = reader.IsDBNull(reader.GetOrdinal("CARI_UNVANI")) ? null : reader["CARI_UNVANI"].ToString(),
                    BelgeNo = reader.IsDBNull(reader.GetOrdinal("BELGE_NO")) ? null : reader["BELGE_NO"].ToString(),
                    GonderiSekli = reader.IsDBNull(reader.GetOrdinal("GONDERI_SEKLI")) ? null : reader["GONDERI_SEKLI"].ToString(),
                    SiparisTarihi = Convert.ToDateTime(reader["SIPARIS_TARIHI"]),
                    OzelKod = reader.IsDBNull(reader.GetOrdinal("OZEL_KOD")) ? null : reader["OZEL_KOD"].ToString(),
                    TemsilciKodu = reader.IsDBNull(reader.GetOrdinal("TEMSILCI_KODU")) ? null : reader["TEMSILCI_KODU"].ToString(),
                    Gonderen = reader.IsDBNull(reader.GetOrdinal("GONDEREN")) ? null : reader["GONDEREN"].ToString(),
                    FisNo = reader.IsDBNull(reader.GetOrdinal("FIS_NO")) ? null : reader["FIS_NO"].ToString(),
                    Depo = reader.IsDBNull(reader.GetOrdinal("DEPO")) ? null : reader["DEPO"].ToString(),
                    SiparisNotu = reader.IsDBNull(reader.GetOrdinal("SIPARIS_NOTU")) ? null : reader["SIPARIS_NOTU"].ToString(),
                    KayitTarihi = reader.IsDBNull(reader.GetOrdinal("KAYIT_TARIHI")) ? (DateTime?)null : Convert.ToDateTime(reader["KAYIT_TARIHI"]),
                    Aktarim = SafeToInt(reader["AKTARIM"]),
                    Isk1 = SafeToDecimal(reader["ISK1"]),
                    Isk2 = SafeToDecimal(reader["ISK2"]),
                    Isk3 = SafeToDecimal(reader["ISK3"]),
                    DbName = reader.IsDBNull(reader.GetOrdinal("DBNAME")) ? null : reader["DBNAME"].ToString(),
                    Firma = SafeToInt(reader["FIRMA"]),
                    Donem = SafeToInt(reader["DONEM"]),
                    Perakende = SafeToInt(reader["PERAKENDE"])
                });
            }

            return orders;
        }

        /// <summary>
        /// Sipariş detay satırlarını FIS_NO'ya göre getirir
        /// </summary>
        public async Task<List<PuntoOrderDetailDto>> GetOrderDetailsAsync(string fisNo)
        {
            var details = new List<PuntoOrderDetailDto>();

            const string sql = @"
                SELECT 
                    [ID], [SIPARIS_NO], [URUN_KODU], [URUN_ADI], [MIKTAR],
                    [BIRIM_FIYAT], [ISK1], [ISK2], [ISK3], [ISK4],
                    [ISK_KAMPANYA], [ISK_CEP], [VADE], [BIRIM], [FIS_NO],
                    [KAYIT_TARIHI], [PESIN_ISK]
                FROM [dbo].[ERYAZ_SIPARIS_DETAY]
                WHERE [FIS_NO] = @FisNo
                ORDER BY [ID]";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FisNo", fisNo);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                details.Add(new PuntoOrderDetailDto
                {
                    Id = Convert.ToInt32(reader["ID"]),
                    SiparisNo = reader.IsDBNull(reader.GetOrdinal("SIPARIS_NO")) ? null : reader["SIPARIS_NO"].ToString(),
                    UrunKodu = reader.IsDBNull(reader.GetOrdinal("URUN_KODU")) ? null : reader["URUN_KODU"].ToString(),
                    UrunAdi = reader.IsDBNull(reader.GetOrdinal("URUN_ADI")) ? null : reader["URUN_ADI"].ToString(),
                    Miktar = SafeToDecimal(reader["MIKTAR"]) ?? 0,
                    BirimFiyat = SafeToDecimal(reader["BIRIM_FIYAT"]) ?? 0,
                    Isk1 = SafeToDecimal(reader["ISK1"]),
                    Isk2 = SafeToDecimal(reader["ISK2"]),
                    Isk3 = SafeToDecimal(reader["ISK3"]),
                    Isk4 = SafeToDecimal(reader["ISK4"]),
                    IskKampanya = SafeToDecimal(reader["ISK_KAMPANYA"]),
                    IskCep = SafeToDecimal(reader["ISK_CEP"]),
                    Vade = SafeToInt(reader["VADE"]),
                    Birim = reader.IsDBNull(reader.GetOrdinal("BIRIM")) ? null : reader["BIRIM"].ToString(),
                    FisNo = reader.IsDBNull(reader.GetOrdinal("FIS_NO")) ? null : reader["FIS_NO"].ToString(),
                    KayitTarihi = reader.IsDBNull(reader.GetOrdinal("KAYIT_TARIHI")) ? (DateTime?)null : Convert.ToDateTime(reader["KAYIT_TARIHI"]),
                    PesinIsk = SafeToDecimal(reader["PESIN_ISK"])
                });
            }

            return details;
        }

        /// <summary>
        /// PUNTO'da siparişi aktarıldı olarak işaretler (AKTARIM=50)
        /// </summary>
        public async Task MarkOrderAsTransferredAsync(int puntoId, string batchGuid)
        {
            const string sql = @"
                UPDATE [dbo].[ERYAZ_SIPARISLER]
                SET [AKTARIM] = 50,
                    [AKTARIM_TARIHI] = GETDATE(),
                    [AKTARIM_GUID] = @BatchGuid
                WHERE [ID] = @PuntoId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@PuntoId", puntoId);
            command.Parameters.AddWithValue("@BatchGuid", batchGuid);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        #endregion

        // =============================================
        // PuntoRepository.cs - SADECE Sanal POS Region DEĞİŞTİ
        // Diğer region'lar (Tahsilat, Sipariş, Helpers) aynı kalıyor
        // =============================================

        // DEĞİŞEN KISIM: #region Sanal POS Methods

        #region Sanal POS Methods

        /// <summary>
        /// PUNTO View'ından aktarılmayı bekleyen SanalPOS kayıtlarını getirir
        /// View: PNTV_ERYAZ_SANALPOS_AKTILACAKLAR
        /// </summary>
        public async Task<List<SanalPosReceiptDto>> GetPendingSanalPosAsync(int batchSize)
        {
            var results = new List<SanalPosReceiptDto>();

            const string sql = @"
        SELECT TOP (@BatchSize)
            [ID],
            [NEREDEN],
            [BELGE_NO],
            [TUR],
            [TARIH],
            [CARI_KODU],
            [CARI_UNVANI],
            [FISNO],
            [TUTAR],
            [PLASIYER_KODU],
            [ORGANIZASYON],
            [BANKA_HESAP_KODU],
            [BANKA_HESAP_ACIKLAMA],
            [ODEME_PLANI_KODU],
            [AKTARIM]
        FROM [PUNTO].[dbo].[PNTV_ERYAZ_SANALPOS_AKTILACAKLAR]";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@BatchSize", batchSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new SanalPosReceiptDto
                {
                    Id = Convert.ToInt32(reader["ID"]),
                    Nereden = reader["NEREDEN"]?.ToString() ?? "",
                    BelgeNo = reader["BELGE_NO"]?.ToString() ?? "",
                    Tur = reader["TUR"]?.ToString() ?? "",
                    Tarih = Convert.ToDateTime(reader["TARIH"]),
                    CariKodu = reader["CARI_KODU"]?.ToString() ?? "",
                    CariUnvani = reader["CARI_UNVANI"]?.ToString() ?? "",
                    FisNo = reader["FISNO"]?.ToString() ?? "",
                    Tutar = Convert.ToDecimal(reader["TUTAR"]),
                    PlasiyerKodu = reader["PLASIYER_KODU"]?.ToString() ?? "",
                    Organizasyon = reader["ORGANIZASYON"]?.ToString() ?? "",
                    BankaHesapKodu = reader["BANKA_HESAP_KODU"]?.ToString() ?? "",
                    BankaHesapAciklama = reader["BANKA_HESAP_ACIKLAMA"]?.ToString() ?? "",
                    OdemePlaniKodu = reader["ODEME_PLANI_KODU"]?.ToString() ?? "",
                    Aktarim = reader["AKTARIM"] == DBNull.Value ? 0 : Convert.ToInt32(reader["AKTARIM"])
                });
            }

            return results;
        }

        /// <summary>
        /// Başarılı POST sonrası ERYAZ_SANALPOS tablosunda AKTARIM=50 set eder
        /// </summary>
        public async Task UpdateSanalPosAktarimAsync(int id)
        {
            const string sql = @"
        UPDATE [PUNTO].[dbo].[ERYAZ_SANALPOS]
        SET [AKTARIM] = 50
        WHERE [ID] = @Id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        #endregion


        #region Helpers

        private static int? SafeToInt(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            var str = value.ToString().Trim();
            if (string.IsNullOrEmpty(str)) return null;
            return int.TryParse(str, out var result) ? result : null;
        }

        private static decimal? SafeToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            var str = value.ToString().Trim();
            if (string.IsNullOrEmpty(str)) return null;
            return decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
        }

        #endregion

        #region Sales Invoice Methods (Faturalama)

        /// <summary>
        /// Faturalaması bekleyen irsaliye detay satırlarını okur.
        /// Filtre: BILLED=0, BILLSTATUS=0
        /// </summary>
        public async Task<List<SalesInvoiceDetailDto>> GetPendingInvoiceDetailsAsync()
        {
            var result = new List<SalesInvoiceDetailDto>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
        SELECT [IRSALIYE_NO]
              ,[IRSALIYE_REF]
              ,[IRSALIYE_TARIHI]
              ,[IRSALIYE_SATIR_REF]
              ,[SIPARIS_NO]
              ,[SIPARIS_TARIHI]
              ,[SIPARIS_REF]
              ,[SIPARIS_SATIR_REF]
              ,[MIKTAR]
              ,[FIYAT]
              ,[FIYAT_KUR]
              ,[CARI_KOD]
              ,[CARI]
              ,[URUN_KODU]
              ,[URUN]
              ,[ODEME_PLANI_REF]
              ,[ODEME_GUNU]
              ,[ODEME_TARIHI]
              ,[ODEME_PLANI]
              ,[SATIR_ODEME_PLANI]
              ,[ISYERI]
              ,[AMBAR]
              ,[BIRIM]
              ,[SATIS_ELEMANI]
              ,[BOSTATUS]
              ,[BILLSTATUS]
              ,[LINETYPE]
              ,[CONTRANSREF]
              ,[PARENTLINEREF]
              ,[DETAILLINENR]
              ,[DETLINE]
              ,[SLIPTYPE]
              ,[MMSLIPLNNR]
              ,[WORKORDERREF]
              ,[WOASSETREF]
              ,[IOCATEGORY]
              ,[INVOICEREF]
              ,[INVOICELNNR]
              ,[ITEMREF]
              ,[ARPREF]
              ,[ORDTRANSREF]
              ,[ORDSLIPREF]
              ,[GLOBTRANS]
              ,[CALCTYPE]
              ,[PRDORDERREF]
              ,[PROMOTIONREF]
              ,[TOTAL]
              ,[PCTYPE]
              ,[PCPRICE]
              ,[PCRATE]
              ,[TCTYPE]
              ,[TCRATE]
              ,[RCRATE]
              ,[DISTCOST]
              ,[DISTDISC]
              ,[DISTEXP]
              ,[DISTPROM]
              ,[DISCPER]
              ,[LINEEXP]
              ,[UOMREF]
              ,[UOMSETREF]
              ,[UINFO1]
              ,[UINFO2]
              ,[VATINC]
              ,[VATRATE]
              ,[VATAMNT]
              ,[VATMATRAH]
              ,[BILLEDITEM]
              ,[BILLED]
              ,[TRASSETTYPE]
              ,[LINENET]
        FROM [PUNTO].[dbo].[PNTV_005_IrsaliyeDetay_Faturala]
        WHERE [BILLED] = 0
          AND [BILLSTATUS] = 0
        ORDER BY [CARI_KOD], [IRSALIYE_NO], [DETAILLINENR]";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 120;
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var dto = new SalesInvoiceDetailDto
                {
                    // ── İrsaliye ──────────────────────────────────────
                    IrsaliyeNo = Convert.ToString(reader["IRSALIYE_NO"]) ?? "",
                    IrsaliyeRef = Convert.ToInt64(reader["IRSALIYE_REF"]),
                    IrsaliyeTarihi = Convert.ToDateTime(reader["IRSALIYE_TARIHI"]),
                    IrsaliyeSatirRef = Convert.ToInt64(reader["IRSALIYE_SATIR_REF"]),

                    // ── Sipariş ───────────────────────────────────────
                    SiparisNo = Convert.ToString(reader["SIPARIS_NO"]) ?? "",
                    SiparisTarihi = reader["SIPARIS_TARIHI"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["SIPARIS_TARIHI"]),
                    SiparisRef = Convert.ToInt64(reader["SIPARIS_REF"]),
                    SiparisSatirRef = Convert.ToInt64(reader["SIPARIS_SATIR_REF"]),

                    // ── Miktar / Fiyat ────────────────────────────────
                    Miktar = Convert.ToDecimal(reader["MIKTAR"]),
                    Fiyat = Convert.ToDecimal(reader["FIYAT"]),
                    FiyatKur = Convert.ToInt32(reader["FIYAT_KUR"]),

                    // ── Cari ──────────────────────────────────────────
                    CariKod = Convert.ToString(reader["CARI_KOD"]) ?? "",
                    Cari = Convert.ToString(reader["CARI"]) ?? "",

                    // ── Ürün ──────────────────────────────────────────
                    UrunKodu = Convert.ToString(reader["URUN_KODU"]) ?? "",
                    Urun = Convert.ToString(reader["URUN"]) ?? "",

                    // ── Ödeme ─────────────────────────────────────────
                    OdemePlaniRef = Convert.ToInt64(reader["ODEME_PLANI_REF"]),
                    OdemeGunu = Convert.ToInt32(reader["ODEME_GUNU"]),
                    OdemeTarihi = reader["ODEME_TARIHI"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["ODEME_TARIHI"]),
                    OdemePlani = Convert.ToString(reader["ODEME_PLANI"]) ?? "",
                    SatirOdemePlani = reader["SATIR_ODEME_PLANI"] == DBNull.Value
                        ? null
                        : Convert.ToString(reader["SATIR_ODEME_PLANI"]),

                    // ── Organizasyon ──────────────────────────────────
                    Isyeri = Convert.ToString(reader["ISYERI"]) ?? "",
                    Ambar = Convert.ToString(reader["AMBAR"]) ?? "",
                    Birim = Convert.ToString(reader["BIRIM"]) ?? "ADET",
                    SatisElemani = Convert.ToString(reader["SATIS_ELEMANI"]) ?? "",

                    // ── Durum & Tip ───────────────────────────────────
                    BoStatus = Convert.ToInt32(reader["BOSTATUS"]),
                    BillStatus = Convert.ToInt32(reader["BILLSTATUS"]),
                    LineType = Convert.ToInt32(reader["LINETYPE"]),
                    SlipType = Convert.ToInt32(reader["SLIPTYPE"]),

                    // ── Logo Referanslar ──────────────────────────────
                    ContTransRef = Convert.ToInt64(reader["CONTRANSREF"]),
                    ParentLineRef = Convert.ToInt64(reader["PARENTLINEREF"]),
                    DetailLineNr = Convert.ToInt32(reader["DETAILLINENR"]),
                    DetLine = Convert.ToInt32(reader["DETLINE"]),
                    MmSlipLnNr = Convert.ToInt32(reader["MMSLIPLNNR"]),
                    WorkOrderRef = Convert.ToInt64(reader["WORKORDERREF"]),
                    WoAssetRef = Convert.ToInt64(reader["WOASSETREF"]),
                    IoCategory = Convert.ToInt32(reader["IOCATEGORY"]),
                    InvoiceRef = Convert.ToInt64(reader["INVOICEREF"]),
                    InvoiceLnNr = Convert.ToInt32(reader["INVOICELNNR"]),
                    ItemRef = Convert.ToInt64(reader["ITEMREF"]),
                    ArpRef = Convert.ToInt64(reader["ARPREF"]),
                    OrdTransRef = Convert.ToInt64(reader["ORDTRANSREF"]),
                    OrdSlipRef = Convert.ToInt64(reader["ORDSLIPREF"]),
                    GlobTrans = Convert.ToInt32(reader["GLOBTRANS"]),
                    CalcType = Convert.ToInt32(reader["CALCTYPE"]),
                    PrdOrderRef = Convert.ToInt64(reader["PRDORDERREF"]),
                    PromotionRef = Convert.ToInt64(reader["PROMOTIONREF"]),

                    // ── Tutarlar ──────────────────────────────────────
                    Total = Convert.ToDecimal(reader["TOTAL"]),
                    PcType = Convert.ToInt32(reader["PCTYPE"]),
                    PcPrice = Convert.ToDecimal(reader["PCPRICE"]),
                    PcRate = Convert.ToDecimal(reader["PCRATE"]),
                    TcType = Convert.ToInt32(reader["TCTYPE"]),
                    TcRate = Convert.ToDecimal(reader["TCRATE"]),
                    RcRate = Convert.ToDecimal(reader["RCRATE"]),
                    DistCost = Convert.ToDecimal(reader["DISTCOST"]),
                    DistDisc = Convert.ToDecimal(reader["DISTDISC"]),
                    DistExp = Convert.ToDecimal(reader["DISTEXP"]),
                    DistProm = Convert.ToDecimal(reader["DISTPROM"]),
                    DiscPer = Convert.ToDecimal(reader["DISCPER"]),
                    LineExp = reader["LINEEXP"] == DBNull.Value
                        ? null
                        : Convert.ToString(reader["LINEEXP"]),

                    // ── Birim ─────────────────────────────────────────
                    UomRef = Convert.ToInt64(reader["UOMREF"]),
                    UomSetRef = Convert.ToInt64(reader["UOMSETREF"]),
                    UInfo1 = Convert.ToDecimal(reader["UINFO1"]),
                    UInfo2 = Convert.ToDecimal(reader["UINFO2"]),

                    // ── KDV ───────────────────────────────────────────
                    VatInc = Convert.ToInt32(reader["VATINC"]),
                    VatRate = Convert.ToDecimal(reader["VATRATE"]),
                    VatAmnt = Convert.ToDecimal(reader["VATAMNT"]),
                    VatMatrah = Convert.ToDecimal(reader["VATMATRAH"]),

                    // ── Faturalama Durumu ─────────────────────────────
                    BilledItem = Convert.ToInt32(reader["BILLEDITEM"]),
                    Billed = Convert.ToInt32(reader["BILLED"]),
                    TrAssetType = Convert.ToInt32(reader["TRASSETTYPE"]),

                    // ── Hesaplanan ────────────────────────────────────
                    LineNet = Convert.ToDecimal(reader["LINENET"])
                };

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Başarılı faturalama sonrası PUNTO tarafında ilgili kayıtları işaretle.
        /// Logo'dan dönen InvoiceRef değeri atanır ve BILLED=1 yapılır.
        /// Not: Bu view olduğu için, gerçek güncelleme hedef tablo üzerinden yapılmalı.
        ///      PUNTO'daki temel tabloya (irsaliye detay) UPDATE atılmalı.
        /// </summary>
        public async Task MarkInvoiceDetailAsBilledAsync(List<long> irsaliyeSatirRefList, long invoiceRef)
        {
            if (!irsaliyeSatirRefList.Any()) return;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // IRSALIYE_SATIR_REF'ler IN clause ile güncellenir
            // TODO: Gerçek tablo adını doğrula (PUNTO DBA ile)
            // Örnek: LG_005_01_STLINE tablosu üzerinden
            var refList = string.Join(",", irsaliyeSatirRefList);

            var sql = $@"
        -- TODO: Gerçek tablo adını doğrula
        -- UPDATE [JGDB05].[dbo].[LG_005_01_STLINE]
        -- SET [INVOICEREF] = @InvoiceRef,
        --     [INVOICELNNR] = 1
        -- WHERE [LOGICALREF] IN ({refList})

        SELECT 1; -- Placeholder
    ";

            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

    }
}
