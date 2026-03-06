namespace MoriiCoffee.Domain.Shared.Enums.Product;

/// <summary>
/// Represents the standardized size options for a product variant.
/// Sizes follow the common coffee-shop convention: Small, Medium, Large, Extra-Large.
/// </summary>
public enum EProductSize
{
    /// <summary>Small size (e.g., 8 oz cup).</summary>
    Small = 0,

    /// <summary>Medium size (e.g., 12 oz cup).</summary>
    Medium = 1,

    /// <summary>Large size (e.g., 16 oz cup).</summary>
    Large = 2,

    /// <summary>Extra-large size (e.g., 20 oz cup).</summary>
    ExtraLarge = 3
}
