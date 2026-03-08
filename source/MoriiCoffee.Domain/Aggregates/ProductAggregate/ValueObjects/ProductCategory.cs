using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.SeedWork.Entities;
using Microsoft.EntityFrameworkCore;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;

[Table("ProductCategories")]
[PrimaryKey("CategoryId", "ProductId")]
public class ProductCategory : EntityBase
{
    [Required] 
    public required Guid CategoryId { get; set; } = Guid.Empty;

    [Required] 
    public required Guid ProductId { get; set; } = Guid.Empty;

    [ForeignKey("CategoryId")]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual Category Category { get; set; } = null!;

    [ForeignKey("ProductId")]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual Product Product { get; set; } = null!;
}
