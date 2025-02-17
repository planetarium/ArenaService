using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Models.BattleTicket;

[Table("battle_ticket_statuses_per_round")]
[Index(nameof(AvatarAddress), nameof(SeasonId), nameof(RoundId), IsUnique = true)]
public class BattleTicketStatusPerRound : TicketStatus
{
    [Required]
    public int RoundId { get; set; }

    [ForeignKey(nameof(RoundId))]
    public Round Round { get; set; } = null!;

    [Required]
    public int BattleTicketPolicyId { get; set; }

    [ForeignKey(nameof(BattleTicketPolicyId))]
    public BattleTicketPolicy BattleTicketPolicy { get; set; } = null!;

    [Required]
    public int RemainingCount { get; set; }

    public int WinCount { get; set; } = 0;

    public int LoseCount { get; set; } = 0;
}
