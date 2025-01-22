namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;
using ArenaService.Models.BattleTicket;

public static class BattleTicketPurchaseLogExtensions
{
    public static TicketPurchaseLogResponse ToResponse(this BattleTicketPurchaseLog log)
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
