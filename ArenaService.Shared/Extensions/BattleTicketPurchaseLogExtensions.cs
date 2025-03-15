namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;

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
            TxStatus = log.TxStatus is null ? Models.Enums.TxStatus.INVALID : log.TxStatus,
        };
    }
}
