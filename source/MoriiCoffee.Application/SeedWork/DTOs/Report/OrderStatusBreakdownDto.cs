namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// Order-status breakdown section returned to the reports client.
/// </summary>
public class OrderStatusBreakdownDto
{
    /// <summary>Total number of orders created during the selected reporting range.</summary>
    public int TotalOrders { get; set; }

    /// <summary>Grouped order-status rows.</summary>
    public List<OrderStatusBreakdownItemDto> Items { get; set; } = [];
}

/// <summary>
/// One grouped item in the order-status breakdown.
/// </summary>
public class OrderStatusBreakdownItemDto
{
    /// <summary>Current order status.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Count of orders in the status group.</summary>
    public int Count { get; set; }

    /// <summary>Share of the total orders represented by this status.</summary>
    public decimal Percentage { get; set; }
}
