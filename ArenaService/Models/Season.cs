using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

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
    public int PricePolicyId { get; set; }

    [ForeignKey(nameof(PricePolicyId))]
    public RefreshPricePolicy PricePolicy { get; set; } = null!;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Round> Rounds { get; set; } = null!;
}
