using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public int Interval { get; set; } = 100;

    public ICollection<Participant> Participants { get; set; } = null!;
    public ICollection<Round> Rounds { get; set; } = null!;
}
