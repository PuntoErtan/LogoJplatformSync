using System;
using System.Collections.Generic;
using LogoSync.Core.DTOs;

namespace LogoSync.Core.Mappers
{
    /// <summary>
    /// PUNTO sipariş verilerini Logo J-Platform salesOrder endpoint formatına dönüştürür
    /// </summary>
    public static class OrderMapper
    {
        // =========================================================
        // Yeni akış: Doğrudan PUNTO → Logo (SRC_Orders ara tablosu yok)
        // =========================================================

        /// <summary>
        /// PuntoOrderDto + PuntoOrderDetailDto listesinden doğrudan JplatformOrderSlip oluşturur.
        /// SRC_Orders ara tablosuna gerek kalmadan PUNTO verilerini Logo formatına dönüştürür.
        /// </summary>
        public static JplatformOrderSlip MapFromPunto(
            PuntoOrderDto order,
            List<PuntoOrderDetailDto> details,
            string warehouse,
            string orgUnit,
            string salespersonCode,
            string paymentPlan)
        {
            var now = DateTime.Now;
            var dateStr = order.SiparisTarihi.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss") + ".000+03:00";
            var nowStr = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss") + ".000+03:00";

            var slip = new JplatformOrderSlip
            {
                No = order.FisNo,
                Date = dateStr,
                Time = new TimeDto
                {
                    Hour = now.Hour,
                    Minute = now.Minute,
                    Second = 0,
                    Milisecond = 0
                },
                DocumentNo = order.BelgeNo ?? "",
                DocumentDate = nowStr,
                OrgUnit = orgUnit ?? "",
                Warehouse = warehouse ?? "",
                Arap = order.CariKodu,
                SalespersonCode = salespersonCode ?? "",
                PaymentPlan = paymentPlan,
                Code4 = order.CariKodu,
                TradingGroup = null,
                DocumentTracking = DeriveDocumentTracking(order.FisNo),
                SendingDate = nowStr,
                Earchive = false,
                Einvoice = false,
                OnlineSalesInvoice = false,
                OrderDiscountDTO = BuildPuntoOrderLines(details, salespersonCode, warehouse, dateStr)
            };

            return slip;
        }

        /// <summary>
        /// FIS_NO'dan DocumentTracking değerini türetir.
        /// Baştaki rakam olmayan karakterleri kaldırır.
        /// Örnek: "D1227103" → "1227103"
        /// </summary>
        public static string DeriveDocumentTracking(string fisNo)
        {
            if (string.IsNullOrEmpty(fisNo))
                return null;

            int firstDigitIndex = -1;
            for (int i = 0; i < fisNo.Length; i++)
            {
                if (char.IsDigit(fisNo[i]))
                {
                    firstDigitIndex = i;
                    break;
                }
            }

            if (firstDigitIndex < 0)
                return null;

            return fisNo.Substring(firstDigitIndex);
        }

        /// <summary>
        /// PUNTO detay satırlarından sipariş satırlarını oluşturur.
        /// Her malzeme satırı (type=0) ardından ISK3, ISK4, ISK_KAMPANYA, ISK_CEP iskonto satırları (type=2) gelir.
        /// </summary>
        private static List<OrderLineDto> BuildPuntoOrderLines(
            List<PuntoOrderDetailDto> details,
            string salespersonCode,
            string warehouse,
            string dateStr)
        {
            var lines = new List<OrderLineDto>();

            foreach (var detail in details)
            {
                // 1. Malzeme satırı (type=0)
                lines.Add(new OrderLineDto
                {
                    OrderAnalysisDTO = new List<OrderAnalysisDto> { CreateDefaultAnalysis() },
                    Deep = false,
                    Type = 0,
                    Code = detail.UrunKodu,
                    Quantity = detail.Miktar,
                    UndeliveredQuantity = detail.Miktar,
                    Unit = 29,
                    UnitCode = detail.Birim ?? "ADET",
                    UnitPrice = detail.BirimFiyat,
                    CurrencyTypeRC = 1,
                    VatratePercent = 20.0m,
                    VatIncluded = false,
                    NetDiscount = false,
                    GstIncluded = false,
                    Amount = detail.BirimFiyat * detail.Miktar,
                    AdditionalTaxIncluded = false,
                    ProcurementDate = dateStr,
                    Reserved = false,
                    Status = 1,
                    PurchaseEmployeeSalespersonCode = salespersonCode ?? "",
                    Warehouse = warehouse ?? "",
                    SubjectToInspection = false,
                    Index = 0,
                    SerializeNulls = false
                });

                // 2. ISK3 iskonto satırı (type=2)
                if (detail.Isk3.HasValue && detail.Isk3.Value > 0)
                    lines.Add(CreateDiscountLine(detail.Isk3.Value, salespersonCode, warehouse));

                // 3. ISK4 iskonto satırı (type=2)
                if (detail.Isk4.HasValue && detail.Isk4.Value > 0)
                    lines.Add(CreateDiscountLine(detail.Isk4.Value, salespersonCode, warehouse));

                // 4. ISK_KAMPANYA iskonto satırı (type=2)
                if (detail.IskKampanya.HasValue && detail.IskKampanya.Value > 0)
                    lines.Add(CreateDiscountLine(detail.IskKampanya.Value, salespersonCode, warehouse));

                // 5. ISK_CEP iskonto satırı (type=2)
                if (detail.IskCep.HasValue && detail.IskCep.Value > 0)
                    lines.Add(CreateDiscountLine(detail.IskCep.Value, salespersonCode, warehouse));
            }

            return lines;
        }

        // =========================================================
        // Eski akış: OrderDto üzerinden (SRC_Orders ara tablosu)
        // Üretimde doğrudan akış doğrulandıktan sonra silinecek.
        // =========================================================

        /// <summary>
        /// [Obsolete] OrderDto'yu Logo formatına dönüştürür - Yeni kodda MapFromPunto kullanın
        /// </summary>
        [Obsolete("MapFromPunto kullanın. SRC_Orders ara tablosu kaldırıldı.")]
        public static JplatformOrderSlip MapToJplatformOrder(OrderDto order)
        {
            var now = DateTime.Now;
            var dateStr = order.OrderDate.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss") + ".000+03:00";
            var nowStr = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss") + ".000+03:00";

            var slip = new JplatformOrderSlip
            {
                No = order.FisNo,
                Date = dateStr,
                Time = new TimeDto
                {
                    Hour = now.Hour,
                    Minute = now.Minute,
                    Second = 0,
                    Milisecond = 0
                },
                DocumentNo = order.DocumentNo ?? "",
                DocumentDate = nowStr,
                OrgUnit = order.OrgUnit ?? "",
                Warehouse = order.Warehouse ?? "",
                Arap = order.CustomerCode,
                SalespersonCode = order.SalespersonCode ?? "",
                PaymentPlan = order.PaymentPlan,
                Code4 = order.CustomerCode,
                TradingGroup = order.TradingGroup,
                DocumentTracking = order.DocumentTracking,
                SendingDate = nowStr,
                Earchive = false,
                Einvoice = false,
                OnlineSalesInvoice = false,
                OrderDiscountDTO = BuildOrderLines(order, dateStr)
            };

            return slip;
        }

        /// <summary>
        /// Sipariş satırlarını oluşturur.
        /// Her malzeme satırı (type=0) ardından iskonto satırları (type=2) gelir.
        /// </summary>
        private static List<OrderLineDto> BuildOrderLines(OrderDto order, string dateStr)
        {
            var lines = new List<OrderLineDto>();
            var defaultAnalysis = CreateDefaultAnalysis();

            foreach (var detail in order.Details)
            {
                // 1. Malzeme satırı (type=0)
                var materialLine = new OrderLineDto
                {
                    OrderAnalysisDTO = new List<OrderAnalysisDto> { CreateDefaultAnalysis() },
                    Deep = false,
                    Type = 0,
                    Code = detail.ProductCode,
                    Quantity = detail.Quantity,
                    UndeliveredQuantity = detail.Quantity,
                    Unit = 29,
                    UnitCode = detail.UnitCode ?? "ADET",
                    UnitPrice = detail.UnitPrice,
                    CurrencyTypeRC = 1,
                    VatratePercent = 20.0m,
                    VatIncluded = false,
                    NetDiscount = false,
                    GstIncluded = false,
                    Amount = detail.UnitPrice * detail.Quantity,
                    AdditionalTaxIncluded = false,
                    ProcurementDate = dateStr,
                    Reserved = false,
                    Status = 1,
                    PurchaseEmployeeSalespersonCode = order.SalespersonCode ?? "",
                    Warehouse = order.Warehouse ?? "",
                    SubjectToInspection = false,
                    Index = 0,
                    SerializeNulls = false
                };

                lines.Add(materialLine);

                // 2. ISK_ISK3 iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountIsk3.HasValue && detail.DiscountIsk3.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountIsk3.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }

                // 3. ISK_ISK4 iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountIsk4.HasValue && detail.DiscountIsk4.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountIsk4.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }

                // 4. ISK_KAMPANYA iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountCampaign.HasValue && detail.DiscountCampaign.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountCampaign.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }

                // 5. ISK_CEP iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountMobile.HasValue && detail.DiscountMobile.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountMobile.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }
            }

            return lines;
        }

        /// <summary>
        /// İskonto satırı (type=2) oluşturur
        /// </summary>
        private static OrderLineDto CreateDiscountLine(decimal percent, string salespersonCode, string warehouse)
        {
            return new OrderLineDto
            {
                OrderAnalysisDTO = new List<OrderAnalysisDto> { CreateDefaultAnalysis() },
                Deep = false,
                Type = 2,
                Code = "<..>",
                Description = "",
                Quantity = 0,
                UndeliveredQuantity = 0,
                Unit = 0,
                UnitCode = "",
                UnitPrice = 0,
                CurrencyTypeRC = 1,
                Percent = percent,
                VatratePercent = 0,
                VatIncluded = false,
                NetDiscount = false,
                GstIncluded = false,
                Amount = 0,
                AdditionalTaxIncluded = false,
                Reserved = false,
                Status = 1,
                PurchaseEmployeeSalespersonCode = salespersonCode ?? "",
                Warehouse = warehouse ?? "",
                SubjectToInspection = false,
                Index = 0,
                SerializeNulls = false
            };
        }

        /// <summary>
        /// Varsayılan boş analiz boyutu oluşturur
        /// </summary>
        private static OrderAnalysisDto CreateDefaultAnalysis()
        {
            return new OrderAnalysisDto
            {
                AnalysisDimensionCode = "",
                AnalysisDimensionDescription = "",
                ProjectCode = "",
                ProjectDescription = "",
                ProjectActivityCode = "",
                ProjectActivityDescription = "",
                DistributionRate = 0,
                Amount = 0,
                AmountRC = 0,
                AmountTC = 0,
                Index = 0,
                SerializeNulls = false
            };
        }

        /// <summary>
        /// DEPO alanından OrgUnit türetir: "7" → "01.7", "42" → "01.42"
        /// Eğer zaten "01.7" formatındaysa dokunmaz
        /// </summary>
        public static string DeriveOrgUnit(string warehouse)
        {
            if (string.IsNullOrEmpty(warehouse))
                return "";

            // Zaten "01.7" formatındaysa dokunma
            if (warehouse.StartsWith("01."))
                return warehouse;

            return $"01.{warehouse.Trim()}";
        }
    }
}
