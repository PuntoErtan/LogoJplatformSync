using System;
using System.Collections.Generic;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// JGDB05.SRC_Orders tablosundaki sipariş başlık verisi
    /// </summary>
    public class OrderDto
    {
        public long Id { get; set; }
        public string FisNo { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string DocumentNo { get; set; }
        public string ShipmentMethod { get; set; }
        public string SalespersonCode { get; set; }
        public string Warehouse { get; set; }
        public string OrgUnit { get; set; }
        public string OrderNote { get; set; }
        public string SpecialCode { get; set; }
        public string PaymentPlan { get; set; }
        public bool IsSynced { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? PuntoId { get; set; }

        public List<OrderDetailDto> Details { get; set; } = new List<OrderDetailDto>();
    }

    /// <summary>
    /// JGDB05.SRC_OrderDetails tablosundaki sipariş satır verisi
    /// </summary>
    public class OrderDetailDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitCode { get; set; }
        public decimal? DiscountCampaign { get; set; }
        public decimal? DiscountMobile { get; set; }
        public decimal? DiscountIsk3 { get; set; }
        public decimal? DiscountIsk4 { get; set; }
        public int LineOrder { get; set; }
    }
}
