using System.ComponentModel;
using MoriiCoffee.Domain.Shared.Enums.Common;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Domain.Shared.SeedWork;

/// <summary>
/// Represents the filter parameters used for pagination, ordering, and searching in a paginated query.
/// </summary>
public class PaginationFilter
{
    private int _page = 1;
    private int _size = 10;
    private List<Order> _orders = new();
    private List<Search> _searches = new();

    [DefaultValue(1)]
    [SwaggerSchema("Current page number (min: 1)")]
    [FromQuery(Name = "page")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [DefaultValue(10)]
    [SwaggerSchema("Number of items per page (min: 1)")]
    [FromQuery(Name = "size")]
    public int Size
    {
        get => _size;
        set => _size = value < 1 ? 1 : value;
    }

    [SwaggerSchema("List of ordering criteria (attribute + direction)")]
    [FromQuery(Name = "orders")]
    public IEnumerable<Order> Orders
    {
        get => _orders;
        set => _orders = value.ToList();
    }

    [SwaggerSchema("List of search criteria (attribute + value)")]
    [FromQuery(Name = "searches")]
    public IEnumerable<Search> Searches
    {
        get => _searches;
        set => _searches = value.ToList();
    }

    [DefaultValue(false)]
    [SwaggerSchema("When true, returns all items without pagination")]
    [FromQuery(Name = "takeAll")]
    public bool TakeAll { get; set; }
}

/// <summary>Represents an ordering criterion for a paginated query.</summary>
public class Order
{
    [DefaultValue(null)]
    public string? OrderBy { get; set; }

    [DefaultValue(EPageOrder.ASC)]
    public EPageOrder OrderDirection { get; set; }
}

/// <summary>Represents a search criterion for a paginated query.</summary>
public class Search
{
    [DefaultValue(null)]
    public string? SearchBy { get; set; }

    [DefaultValue(null)]
    public string? SearchValue { get; set; }
}

/// <summary>Extended pagination filter for products with category and status filtering.</summary>
public class ProductPaginationFilter : PaginationFilter
{
    [FromQuery(Name = "categoryIds")]
    public List<Guid>? CategoryIds { get; set; }

    [FromQuery(Name = "isFeatured")]
    public bool? IsFeatured { get; set; }
}
