using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

namespace ArenaService.Models;

[Table("ticket_price")]
public class TicketPrice
{
    public int Id { get; set; }

    [Required]
    public long Price { get; }

    [Required]
    public int MaxTicketPurchaseCount { get; }

    [Required]
    public int MaxPurchaseCountEachRound { get; }

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
