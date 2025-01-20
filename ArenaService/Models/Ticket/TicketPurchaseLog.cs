using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;

namespace ArenaService.Models.Ticket;

[Table("ticket_purchase_logs")]
public class TicketPurchaseLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TicketStatusId { get; set; }

    [ForeignKey(nameof(TicketStatusId))]
    public TicketStatus TicketStatus { get; set; } = null!;

    [Required]
    public TicketType TicketType { get; set; }

    [Required]
    public int PurchaseOrder { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Required]
    public int PurchaseCount { get; set; }

    [Required]
    public PurchaseStatus PurchaseStatus { get; set; }

    [Required]
    public required string TxId { get; set; }

    public TxStatus? TxStatus { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
