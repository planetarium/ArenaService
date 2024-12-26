using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("available_opponents")]
public class AvailableOpponent
{
    public int Id { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    [Required]
    public int OpponentId { get; set; }
    public Participant Opponent { get; set; } = null!;

    [Required]
    public long RefillBlockIndex { get; set; }

    public bool IsBattled { get; set; } = false;
}
