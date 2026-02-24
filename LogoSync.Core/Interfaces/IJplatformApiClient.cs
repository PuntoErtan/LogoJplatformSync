using LogoSync.Core.DTOs;

namespace LogoSync.Core.Interfaces;

public interface IJplatformApiClient
{
    /// <summary>
    /// Nakit Tahsilat - Kasa Fişi oluştur
    /// POST /safedepositslips
    /// </summary>
    Task<JplatformApiResponse> CreateCashReceiptAsync(JplatformSafeDepositSlip slip);

    /// <summary>
    /// Çek Giriş Bordrosu oluştur
    /// POST /chequepnoteslips?slipType=1
    /// </summary>
    Task<JplatformApiResponse> CreateChequeReceiptAsync(JplatformChequePNoteSlip slip);

    /// <summary>
    /// Sipariş oluştur
    /// POST /salesOrder
    /// </summary>
    Task<JplatformApiResponse> SendOrderAsync(JplatformOrderSlip orderSlip);

    /// <summary>
    /// Cari Hesap Fişi (Sanal POS) oluştur
    /// POST /arpslips?slipType=08
    /// </summary>
    Task<JplatformApiResponse> CreateArpSlipAsync(JplatformArpSlip slip);


    /// <summary>
    /// İrsaliyeden Satış Faturası oluştur
    /// POST /invoices/sales?invoiceType=8
    /// </summary>
    Task<JplatformApiResponse> CreateSalesInvoiceAsync(JplatformSalesInvoice invoice);

    /// <summary>
    /// Fatura oluşturulduktan sonra kampanya uygula
    /// PUT /invoices/sales/applyCampaign?invoiceType=8&amp;canSaveAppliedCampaign=true
    /// </summary>
    Task<JplatformApiResponse> ApplyCampaignAsync(ApplyCampaignRequest request);
}
