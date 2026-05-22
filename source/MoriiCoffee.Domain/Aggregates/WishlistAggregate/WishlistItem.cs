using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.WishlistAggregate;

/// <summary>
/// Represents a product saved to a user's wishlist.
/// Simple junction record — no soft delete, no domain logic.
/// </summary>
[Table("WishlistItems")]
public class WishlistItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
