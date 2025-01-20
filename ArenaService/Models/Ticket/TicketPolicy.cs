using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;

namespace ArenaService.Models.Ticket;

[Table("ticket_policies")]
public class TicketPolicy
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    [Required]
    public required string Name { get; set; }

    [Required]
    public TicketType TicketType { get; set; }

    [Required]
    public bool IsPricePersistentForSeason { get; set; }

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

    public decimal GetPrice(int purchaseOrder)
    {
        if (purchaseOrder < 1 || purchaseOrder > PurchasePrices.Count)
        {
            throw new InvalidOperationException("Invalid purchase order.");
        }

        return PurchasePrices[purchaseOrder - 1];
    }

    public bool CanPurchaseMore(int currentPurchased)
    {
        return currentPurchased < MaxPurchasableTicketsPerRound;
    }
}
