using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;

namespace ArenaService.Models.BattleTicket;

[Table("battle_ticket_usage_logs")]
public class BattleTicketUsageLog : TicketUsageLog
{
    [Required]
    public int BattleTicketStatusPerRoundId { get; set; }

    [ForeignKey(nameof(BattleTicketStatusPerRoundId))]
    public BattleTicketStatusPerRound BattleTicketStatusPerRound { get; set; } = null!;

    [Required]
    public int BattleTicketStatusPerSeasonId { get; set; }

    [ForeignKey(nameof(BattleTicketStatusPerSeasonId))]
    public BattleTicketStatusPerSeason BattleTicketStatusPerSeason { get; set; } = null!;

    [Required]
    public int BattleId { get; set; }
}
