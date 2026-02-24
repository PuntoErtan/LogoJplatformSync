using System.Text.Json.Serialization;

namespace LogoSync.Core.DTOs
{
    /// <summary>
    /// Fatura oluşturulduktan sonra kampanya uygulama isteği.
    /// PUT /invoices/sales/applyCampaign?invoiceType=8&amp;canSaveAppliedCampaign=true
    ///
    /// no  → POST /invoices/sales response'undaki "code" alanından gelir
    /// date, orgUnit, warehouse, arap → POST body'den alınır
    /// </summary>
    public class ApplyCampaignRequest
    {
        /// <summary>Fatura numarası - POST response'undaki "code" alanı (ör: "SIL-202600000001")</summary>
        [JsonPropertyName("no")]
        public string No { get; set; }

        /// <summary>Fatura tarihi - Logo format: "2026-02-20T10:50:47.000+03:00"</summary>
        [JsonPropertyName("date")]
        public string Date { get; set; }

        /// <summary>İşyeri kodu (ör: "01.7")</summary>
        [JsonPropertyName("orgUnit")]
        public string OrgUnit { get; set; }

        /// <summary>Ambar kodu (ör: "01.7.7")</summary>
        [JsonPropertyName("warehouse")]
        public string Warehouse { get; set; }

        /// <summary>Müşteri (cari) kodu (ör: "M.07002.01.532")</summary>
        [JsonPropertyName("arap")]
        public string Arap { get; set; }
    }
}
