// =============================================
// LogoSync.Core/Interfaces/IPuntoRepository.cs
// =============================================

using LogoSync.Core.DTOs;

namespace LogoSync.Core.Interfaces
{
    public interface IPuntoRepository
    {
        // Tahsilat (Nakit + Çek)
        Task<IEnumerable<PuntoCashReceiptDto>> GetPendingCashReceiptsAsync(int batchSize);
        Task MarkAsTransferredAsync(int puntoId, string batchGuid);

        // Sipariş
        Task<List<PuntoOrderDto>> GetPendingOrdersAsync(int batchSize);
        Task<List<PuntoOrderDetailDto>> GetOrderDetailsAsync(string fisNo);
        Task MarkOrderAsTransferredAsync(int puntoId, string batchGuid);

        // Sanal POS
        Task<List<SanalPosReceiptDto>> GetPendingSanalPosAsync(int batchSize);
        Task UpdateSanalPosAktarimAsync(int id);

        // Sales Invoice (İrsaliyeden Faturalama)
        Task<List<SalesInvoiceDetailDto>> GetPendingInvoiceDetailsAsync();
        Task MarkInvoiceDetailAsBilledAsync(List<long> irsaliyeSatirRefList, long invoiceRef);
    }
}
