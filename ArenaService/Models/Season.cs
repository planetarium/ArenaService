using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;
using ArenaService.Models.BattleTicket;
using ArenaService.Models.RefreshTicket;
using ArenaService.Models.Ticket;
using ArenaService.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("seasons")]
[Index(nameof(StartBlock), nameof(EndBlock))]
[Index(nameof(StartBlock))]
public class Season
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SeasonGroupId { get; set; } = 0;

    [Required]
    public long StartBlock { get; set; }

    [Required]
    public long EndBlock { get; set; }

    [Required]
    public ArenaType ArenaType { get; set; }

    [Required]
    public int RoundInterval { get; set; }

    [Required]
    public int RequiredMedalCount { get; set; }

    [Required]
    public int TotalPrize { get; set; }

    [Required]
    public string PrizeDetailUrl { get; set; } =
        "https://discord.com/channels/539405872346955788/1027763804643262505";

    [Required]
    public int BattleTicketPolicyId { get; set; }

    [ForeignKey(nameof(BattleTicketPolicyId))]
    public BattleTicketPolicy BattleTicketPolicy { get; set; } = null!;

    [Required]
    public int RefreshTicketPolicyId { get; set; }

    [ForeignKey(nameof(RefreshTicketPolicyId))]
    public RefreshTicketPolicy RefreshTicketPolicy { get; set; } = null!;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Round> Rounds { get; set; } = null!;
}
