using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Infrastructure.Services.Order;

/// <summary>
/// Generates daily-sequential order numbers in the format <c>MRC-YYYYMMDD-NNN</c>.
/// The sequence resets each calendar day and is padded to three digits (e.g., 001, 002 …).
/// </summary>
public class OrderIdGenerator : IOrderIdGenerator
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderIdGenerator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"MRC-{today:yyyyMMdd}-";
        var count = await _unitOfWork.Orders.CountByOrderNumberPrefixAsync(prefix);
        return $"{prefix}{count + 1:D3}";
    }
}
