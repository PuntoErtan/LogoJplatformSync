// =============================================
// LogoSync.Core/Mappers/SanalPosMapper.cs
// SanalPosReceiptDto → JplatformArpSlip dönüşümü
// =============================================

using LogoSync.Core.DTOs;

namespace LogoSync.Core.Mappers
{
    public static class SanalPosMapper
    {
        /// <summary>
        /// PUNTO SanalPos kaydını Logo J-Platform ArpSlip formatına dönüştürür
        /// Tarih formatı: "2026-02-17T15:42:26.000+03:00"
        /// slipNo = BELGE_NO
        /// docNo = FISNO
        /// arpInfoDef = CARI_UNVANI
        /// </summary>
        public static JplatformArpSlip MapToArpSlip(SanalPosReceiptDto receipt)
        {
            return new JplatformArpSlip
            {
                SlipDate = receipt.Tarih.ToString("yyyy-MM-ddTHH:mm:ss.fff+03:00"),
                SlipNo = receipt.BelgeNo,
                DocNo = receipt.FisNo,
                OrgUnitCode = receipt.Organizasyon,
                footnote = receipt.CariUnvani,
                SlipTransaction = new List<ArpSlipTransaction>
                {
                    new ArpSlipTransaction
                    {
                        ArpInfoCode = receipt.CariKodu,
                        ArpInfoDef = receipt.CariUnvani,
                        ExchRateDifferenceCurrencyType = -1,
                        LineCredit = receipt.Tutar,
                        ReportingCurrency = "USD",
                        PaymentPlanCode = receipt.OdemePlaniKodu,
                        TransType = -1,
                        EmployeeCode = receipt.PlasiyerKodu,
                        CrossAccType = 2,
                        CrossAccCode = receipt.BankaHesapKodu,
                        CrossAccDef = receipt.BankaHesapAciklama,
                        Index = 0,
                        SerializeNulls = false
                    }
                },
                Index = 0,
                SerializeNulls = false
            };
        }
    }
}
