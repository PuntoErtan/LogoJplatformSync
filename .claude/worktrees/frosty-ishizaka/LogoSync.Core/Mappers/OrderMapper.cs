using System;
using System.Collections.Generic;
using LogoSync.Core.DTOs;

namespace LogoSync.Core.Mappers
{
    /// <summary>
    /// OrderDto → JplatformOrderSlip mapping servisi
    /// PUNTO sipariş verilerini Logo J-Platform orders endpoint formatına dönüştürür
    /// </summary>
    public static class OrderMapper
    {
        /// <summary>
        /// OrderDto'yu Logo J-Platform orders formatına dönüştürür
        /// Her malzeme satırından sonra ISK_KAMPANYA ve ISK_CEP için ayrı type=2 iskonto satırları eklenir
        /// </summary>
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
                SendingDate = nowStr,
                Earchive = false,
                Einvoice = false,
                OnlineSalesInvoice = false,
                OrderDiscountDTO = BuildOrderLines(order)
            };

            return slip;
        }

        /// <summary>
        /// Sipariş satırlarını oluşturur.
        /// Her malzeme satırı (type=0) ardından iskonto satırları (type=2) gelir.
        /// ISK_KAMPANYA > 0 ise ayrı type=2 satırı
        /// ISK_CEP > 0 ise ayrı type=2 satırı
        /// </summary>
        private static List<OrderLineDto> BuildOrderLines(OrderDto order)
        {
            var lines = new List<OrderLineDto>();

            foreach (var detail in order.Details)
            {
                // 1. Malzeme satırı (type=0)
                lines.Add(new OrderLineDto
                {
                    Type = 0,
                    Code = detail.ProductCode,
                    Quantity = detail.Quantity,
                    UndeliveredQuantity = detail.Quantity,
                    UnitCode = detail.UnitCode ?? "ADET",
                    CurrencyTypeRC = 1,
                    VatIncluded = false,
                    Amount = detail.UnitPrice,
                    AdditionalTaxIncluded = false,
                    Reserved = false,
                    Status = 1,
                    PurchaseEmployeeSalespersonCode = order.SalespersonCode ?? "",
                    Warehouse = order.Warehouse ?? "",
                    Index = 0,
                    SerializeNulls = false
                });

                // 2. ISK_ISK3 iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountIsk3.HasValue && detail.DiscountIsk3.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountIsk3.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }

                // 2. ISK_ISK4 iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountIsk4.HasValue && detail.DiscountIsk4.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountIsk4.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }
                // 2. ISK_KAMPANYA iskonto satırı (type=2) - sadece > 0 ise
                if (detail.DiscountCampaign.HasValue && detail.DiscountCampaign.Value > 0)
                {
                    lines.Add(CreateDiscountLine(
                        detail.DiscountCampaign.Value,
                        order.SalespersonCode,
                        order.Warehouse
                    ));
                }

                // 3. ISK_CEP iskonto satırı (type=2) - sadece > 0 ise
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
                Type = 2,
                Code = "<..>",
                Description = "",
                Quantity = 0,
                UndeliveredQuantity = 0,
                UnitCode = "",
                CurrencyTypeRC = 1,
                Percent = percent,
                VatIncluded = false,
                AdditionalTaxIncluded = false,
                Reserved = false,
                Status = 1,
                PurchaseEmployeeSalespersonCode = salespersonCode ?? "",
                Warehouse = warehouse ?? "",
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
