using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;

namespace ArenaService.Models.Ticket;

public abstract class TicketPolicy
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public int DefaultTicketsPerRound { get; set; }

    [Required]
    public int MaxPurchasableTicketsPerRound { get; set; }

    [Required]
    [Column(TypeName = "decimal[]")]
    public List<decimal> PurchasePrices { get; set; } = new();

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public decimal GetPrice(int purchaseCount)
    {
        if (purchaseCount > PurchasePrices.Count)
        {
            throw new InvalidOperationException("Invalid purchase order.");
        }

        return PurchasePrices[purchaseCount];
    }
}
