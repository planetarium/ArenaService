using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

namespace ArenaService.Models;

[Table("seasons")]
public class Season
{
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
    public long EntranceFee { get; }

    [Required]
    [Column("ticket_price_list", TypeName = "float[]")]
    public float[] TicketPriceList { get; set; }

    [Required]
    [Column("refresh_list", TypeName = "float[]")]
    public float[] RefreshPrice { get; }

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Participant> Participants { get; set; } = null!;
    public ICollection<Round> Rounds { get; set; } = null!;
}
