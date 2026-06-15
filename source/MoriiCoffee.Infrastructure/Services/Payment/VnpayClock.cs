using MoriiCoffee.Application.SeedWork.Abstractions;

namespace MoriiCoffee.Infrastructure.Services.Payment;

public sealed class VnpayClock
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public VnpayClock(IDateTimeProvider dateTimeProvider) => _dateTimeProvider = dateTimeProvider;

    public DateTime UtcNow => _dateTimeProvider.UtcNow;

    public string FormatNow() => Format(_dateTimeProvider.UtcNow);

    public string Format(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), VietnamTimeZone)
            .ToString("yyyyMMddHHmmss");

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }
}
