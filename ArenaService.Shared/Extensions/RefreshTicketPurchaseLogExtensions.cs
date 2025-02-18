namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models.RefreshTicket;

public static class RefreshTicketPurchaseLogExtensions
{
    public static TicketPurchaseLogResponse ToResponse(this RefreshTicketPurchaseLog log)
    {
        return new TicketPurchaseLogResponse
        {
            SeasonId = log.SeasonId,
            RoundId = log.RoundId,
            PurchaseCount = log.PurchaseCount,
            PurchaseStatus = log.PurchaseStatus,
            AmountPaid = log.AmountPaid,
            TxId = log.TxId,
            TxStatus = log.TxStatus,
        };
    }
}
