using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Models;

[Table("rounds")]
[Index(nameof(SeasonId), nameof(RoundIndex), IsUnique = true)]
public class Round
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    [Required]
    public long StartBlock { get; set; }

    [Required]
    public long EndBlock { get; set; }

    [Required]
    public int RoundIndex { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
