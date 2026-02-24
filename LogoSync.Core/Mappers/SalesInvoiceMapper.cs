using System;
using System.Collections.Generic;
using System.Linq;
using LogoSync.Core.DTOs;

namespace LogoSync.Core.Mappers
{
    /// <summary>
    /// PNTV_005_IrsaliyeDetay_Faturala view satırlarını
    /// Logo J-Platform SalesInvoice JSON modeline dönüştürür.
    ///
    /// Gruplama: CARI_KOD bazında → aynı müşterinin tüm irsaliye satırları tek fatura.
    /// Endpoint: POST /invoices/sales?invoiceType=8
    /// </summary>
    public static class SalesInvoiceMapper
    {
        /// <summary>
        /// Tüm satırları CARI_KOD bazında gruplar ve her grup için bir fatura oluşturur.
        /// </summary>
        public static List<SalesInvoiceGroup> GroupByCari(List<SalesInvoiceDetailDto> allRows)
        {
            return allRows
                .GroupBy(r => r.CariKod)
                .Select(g => new SalesInvoiceGroup
                {
                    CariKod = g.Key,
                    Rows = g.ToList()
                })
                .ToList();
        }

        /// <summary>
        /// Bir CARI_KOD grubunu JplatformSalesInvoice modeline dönüştürür.
        /// </summary>
        public static JplatformSalesInvoice MapToInvoice(SalesInvoiceGroup group)
        {
            var rows = group.Rows;
            var firstRow = rows.First();
            var now = DateTime.Now;
            var nowLogo = ToLogoDateTimeFormat(now);
            var todayLogo = ToLogoDateFormat(now);

            var invoice = new JplatformSalesInvoice
            {
                // ── Header ────────────────────────────────────────
                SalespersonCode = firstRow.SatisElemani ?? "",
                BoStatus = 1,
                Date = nowLogo,
                Time = new SalesInvoiceTime
                {
                    Hour = now.Hour,
                    Minute = now.Minute,
                    Second = 0,
                    Milisecond = 0
                },
                DocumentDate = nowLogo,
                OrgUnit = firstRow.Isyeri ?? "01.7",
                Warehouse = firstRow.Ambar ?? "01.7.7",
                OrgUnit2 = firstRow.Isyeri ?? "01.7",
                Warehouse2 = firstRow.Ambar ?? "01.7.7",

                // ── Cari ──────────────────────────────────────────
                Arap = firstRow.CariKod,
                ArapTitle = firstRow.Cari ?? "",
                ArapTitle2 = firstRow.Cari ?? "",
                ArapTitle3 = firstRow.Cari ?? "",
                Title = firstRow.Cari ?? "",
                Customer = firstRow.CariKod,
                Customer2 = firstRow.CariKod,
                CodeShipTo = firstRow.CariKod,

                // ── Ödeme Planı ───────────────────────────────────
                PaymentPlan = firstRow.OdemePlani ?? "",
                PaymentPlan2 = firstRow.OdemePlani ?? "",

                // ── Referans Tarih ────────────────────────────────
                ReferenceDate = todayLogo,
            };

            // ── itemTransactionDTO ────────────────────────────────
            invoice.ItemTransactionDTO = BuildItemTransactions(rows);

            // ── masterDataDispatcDTO (benzersiz irsaliyeler) ──────
            invoice.MasterDataDispatcDTO = BuildMasterDataDispatches(rows);

            // ── installmentDTO (KDV dahil toplam) ─────────────────
            invoice.InstallmentDTO = BuildInstallments(rows);

            return invoice;
        }

        // =================================================================
        // itemTransactionDTO oluşturma
        // LINETYPE=0 → malzeme satırı (deep=false)
        // LINETYPE=2 → iskonto satırı (deep=true) — view'dan gelirse veya DISCPER>0 ise
        // =================================================================

        private static List<SalesInvoiceItemTransaction> BuildItemTransactions(List<SalesInvoiceDetailDto> rows)
        {
            var transactions = new List<SalesInvoiceItemTransaction>();

            foreach (var row in rows)
            {
                if (row.LineType == 0)
                {
                    // ── Malzeme satırı ────────────────────────────
                    var itemLine = new SalesInvoiceItemTransaction
                    {
                        Deep = false,
                        Type = 0,
                        LogicalRef = row.IrsaliyeSatirRef,
                        SourceRef = 0,
                        OrderTransRef = row.SiparisSatirRef,
                        OrderSlipRef = row.SiparisRef,
                        DispatchRef = row.IrsaliyeRef,
                        DispatchTransRef = row.SiparisSatirRef,
                        Code = row.UrunKodu ?? "",
                        Quantity = row.Miktar,
                        UnitCode = row.Birim ?? "ADET",
                        UnitPrice = row.Fiyat,
                        CurrencyPC = row.FiyatKur,
                        VatratePercent = row.VatRate,
                        Amount = row.Total,
                        NetAmount = row.Total,
                        ApplyDiscountTransValue = row.Total,
                        Percent = 0.0m,
                        OrderSlipNumber = row.SiparisNo ?? "",
                        OrderDate = row.SiparisTarihi.HasValue
                            ? ToLogoDateTimeFormat(row.SiparisTarihi.Value)
                            : "",
                        DispatchNo = row.IrsaliyeNo ?? "",
                        PaymentPlan = (row.SatirOdemePlani == null || row.SatirOdemePlani == "NULL")
                            ? ""
                            : row.SatirOdemePlani,
                        PurchaseEmployeeSalespersonCode = row.SatisElemani ?? "",
                        Warehouse = row.Ambar ?? "01.7.7"
                    };
                    transactions.Add(itemLine);

                    // ── Hemen ardından iskonto satırı (DISCPER>0 ise) ──
                    if (row.DiscPer > 0)
                    {
                        transactions.Add(CreateDiscountLine(row));
                    }
                }
                else if (row.LineType == 2)
                {
                    // ── View'dan doğrudan gelen iskonto satırı ────
                    transactions.Add(CreateDiscountLine(row));
                }
            }

            return transactions;
        }

        /// <summary>
        /// İskonto satırı oluşturur (type=2, deep=true)
        /// </summary>
        private static SalesInvoiceItemTransaction CreateDiscountLine(SalesInvoiceDetailDto row)
        {
            return new SalesInvoiceItemTransaction
            {
                Deep = true,
                Type = 2,
                LogicalRef = 0,
                SourceRef = 0,
                OrderTransRef = 0,
                OrderSlipRef = 0,
                DispatchRef = 0,
                DispatchTransRef = 0,
                Code = row.UrunKodu ?? "",
                Quantity = 0,
                UnitCode = "",
                UnitPrice = 0,
                CurrencyPC = 0,
                VatratePercent = row.VatRate,
                Percent = row.DiscPer,
                Amount = 0,
                NetAmount = 0,
                ApplyDiscountTransValue = 0,
                OrderSlipNumber = "",
                OrderDate = "",
                DispatchNo = "",
                PaymentPlan = "",
                PurchaseEmployeeSalespersonCode = row.SatisElemani ?? "",
                Warehouse = row.Ambar ?? "01.7.7"
            };
        }

        // =================================================================
        // masterDataDispatcDTO — benzersiz irsaliye başlıkları
        // =================================================================

        private static List<SalesInvoiceMasterDataDispatch> BuildMasterDataDispatches(
            List<SalesInvoiceDetailDto> rows)
        {
            return rows
                .Where(r => !string.IsNullOrEmpty(r.IrsaliyeNo))
                .GroupBy(r => r.IrsaliyeNo)
                .Select(g =>
                {
                    var first = g.First();
                    return new SalesInvoiceMasterDataDispatch
                    {
                        Type = first.SlipType > 0 ? first.SlipType : 8,
                        Number = first.IrsaliyeNo,
                        Date = ToLogoDateTimeFormat(first.IrsaliyeTarihi),
                        DocumenDate = ToLogoDateTimeFormat(first.IrsaliyeTarihi)
                    };
                })
                .ToList();
        }

        // =================================================================
        // installmentDTO — KDV dahil toplam tutar
        // Hesaplama: Her malzeme satırı için → (TOTAL - iskonto) × (1 + VATRATE/100)
        // =================================================================

        private static List<SalesInvoiceInstallment> BuildInstallments(List<SalesInvoiceDetailDto> rows)
        {
            var materialRows = rows.Where(r => r.LineType == 0).ToList();

            if (!materialRows.Any())
                return new List<SalesInvoiceInstallment>();

            decimal totalWithVat = 0;
            foreach (var row in materialRows)
            {
                decimal lineTotal = row.Total; // MIKTAR * FIYAT
                if (row.DiscPer > 0)
                {
                    lineTotal = lineTotal * (1 - row.DiscPer / 100m);
                }
                decimal lineWithVat = lineTotal * (1 + row.VatRate / 100m);
                totalWithVat += lineWithVat;
            }

            // Ödeme tarihi: ilk satırdan
            var paymentDate = materialRows.First().OdemeTarihi ?? DateTime.Now;
            var paymentDateLogo = ToLogoDateFormat(paymentDate);

            return new List<SalesInvoiceInstallment>
            {
                new SalesInvoiceInstallment
                {
                    PaymentNo = "1",
                    Date = paymentDateLogo,
                    OptionDate = paymentDateLogo,
                    DiscountValidation = paymentDateLogo,
                    Amount = Math.Round(totalWithVat, 2)
                }
            };
        }

        // =================================================================
        // Tarih Format Yardımcıları
        // =================================================================

        /// <summary>"2026-02-20T10:50:47.000+03:00"</summary>
        public static string ToLogoDateTimeFormat(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.000") + "+03:00";
        }

        /// <summary>"2026-02-20T00:00:00.000+03:00"</summary>
        public static string ToLogoDateFormat(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddT00:00:00.000") + "+03:00";
        }
    }

    /// <summary>
    /// CARI_KOD bazında gruplandırılmış satırlar
    /// </summary>
    public class SalesInvoiceGroup
    {
        public string CariKod { get; set; }
        public List<SalesInvoiceDetailDto> Rows { get; set; } = new List<SalesInvoiceDetailDto>();
    }
}
