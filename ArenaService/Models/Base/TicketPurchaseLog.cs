using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace ArenaService.Models.Ticket;

public abstract class TicketPurchaseLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [Required]
    public int RoundId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AmountPaid { get; set; }

    [Required]
    public PurchaseStatus PurchaseStatus { get; set; }

    [Required]
    public int PurchaseCount { get; set; }

    [Required]
    public TxId TxId { get; set; }

    public TxStatus? TxStatus { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
