using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Models.BattleTicket;

[Table("battle_ticket_statuses_per_season")]
[Index(nameof(AvatarAddress), nameof(SeasonId), IsUnique = true)]
public class BattleTicketStatusPerSeason : TicketStatus
{
    [Required]
    public int BattleTicketPolicyId { get; set; }

    [ForeignKey(nameof(BattleTicketPolicyId))]
    public BattleTicketPolicy BattleTicketPolicy { get; set; } = null!;
}
