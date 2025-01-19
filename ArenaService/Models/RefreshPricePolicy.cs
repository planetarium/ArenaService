using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("refresh_price_policies")]
public class RefreshPricePolicy
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(255)")]
    public string Name { get; set; } = null!;

    public ICollection<RefreshPriceDetail> RefreshPrices { get; set; } =
        new List<RefreshPriceDetail>();
}
