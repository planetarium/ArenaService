using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;
using ArenaService.Models.Ticket;

namespace ArenaService.Models;

[Table("seasons")]
public class Season
{
    [Key]
    public int Id { get; set; }

    [Required]
    public long StartBlock { get; set; }

    [Required]
    public long EndBlock { get; set; }

    [Required]
    public ArenaType arenaType { get; set; }

    [Required]
    public int RoundInterval { get; set; }

    [Required]
    public int RequiredMedalCount { get; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Round> Rounds { get; set; } = null!;
    public ICollection<TicketPolicy> TicketPolicies { get; set; } = null!;
}
