using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("refresh_price_details")]
public class RefreshPriceDetail
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PolicyId { get; set; }

    [ForeignKey(nameof(PolicyId))]
    public RefreshPricePolicy Policy { get; set; } = null!;

    [Required]
    public int RefreshOrder { get; set; }

    [Required]
    public float Price { get; set; }
}
