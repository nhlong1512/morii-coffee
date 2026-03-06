namespace MoriiCoffee.Domain.Shared.SeedWork;

/// <summary>
/// Represents a paginated result containing a list of items and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the paginated list.</typeparam>
public class Pagination<T>
{
    public List<T> Items { get; set; } = new();
    public Metadata Metadata { get; set; } = new();
}
