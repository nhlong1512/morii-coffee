using MoriiCoffee.Domain.SeedWork.AggregateRoot;

namespace MoriiCoffee.Domain.Aggregates.CategoryAggregate;

/// <summary>
/// Represents a product category (e.g., Coffee, Tea, Food, Cold Brew).
/// Acts as the aggregate root for the Category bounded context.
/// </summary>
public class Category : AggregateRoot
{
    public Guid Id { get; set; }

    /// <summary>Display name of the category (e.g., "Espresso Drinks").</summary>
    public string Name { get; set; } = null!;

    /// <summary>Short description shown to customers.</summary>
    public string? Description { get; set; }

    /// <summary>URL of the icon or thumbnail image for this category.</summary>
    public string? IconUrl { get; set; }

    /// <summary>Sort order for displaying categories in the catalog.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether this category is currently visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;
}
