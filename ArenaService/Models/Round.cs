using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("rounds")]
public class Round
{
    public int Id { get; set; }

    [Required]
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    [Required]
    public long StartBlock { get; set; }

    [Required]
    public long EndBlock { get; set; }
}
