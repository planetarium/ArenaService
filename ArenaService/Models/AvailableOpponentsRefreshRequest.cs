using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("available_opponents_refresh_request")]
public class AvailableOpponentsRefreshRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AvailableOpponentId { get; set; }

    [ForeignKey(nameof(AvailableOpponentId))]
    public AvailableOpponent AvailableOpponent { get; set; } = null!;

    [Required]
    public int RefreshRequestId { get; set; }

    [ForeignKey(nameof(RefreshRequestId))]
    public RefreshRequest RefreshRequest { get; set; } = null!;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
