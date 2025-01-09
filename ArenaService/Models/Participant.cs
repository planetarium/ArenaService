using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("participants")]
[Index(nameof(AvatarAddress))]
public class Participant
{
    [Required]
    public required string AvatarAddress { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public int InitializedScore { get; set; } = 1000;
    public int Score { get; set; } = 1000;
}
