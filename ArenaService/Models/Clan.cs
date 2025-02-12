using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("clans")]
public class Clan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 12)]
    public required string Name { get; set; }

    [Required]
    public required string ImageURL { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = null!;
}
