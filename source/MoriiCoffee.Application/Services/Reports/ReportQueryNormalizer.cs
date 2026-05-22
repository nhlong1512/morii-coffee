using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;

namespace MoriiCoffee.Application.Services.Reports;

/// <summary>
/// Normalizes raw report query parameters into a validated range object used by downstream
/// query handlers and persistence-layer aggregations.
/// </summary>
public class ReportQueryNormalizer
{
    private const string DefaultPreset = "30D";
    private const string CustomPreset = "CUSTOM";
    private const string DefaultTimezone = "Asia/Ho_Chi_Minh";
    private const int MaxRangeDays = 366;

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ComparisonPeriodResolver _comparisonPeriodResolver;

    public ReportQueryNormalizer(
        IDateTimeProvider dateTimeProvider,
        ComparisonPeriodResolver comparisonPeriodResolver)
    {
        _dateTimeProvider = dateTimeProvider;
        _comparisonPeriodResolver = comparisonPeriodResolver;
    }

    /// <summary>
    /// Normalizes the incoming report parameters into a validated query range.
    /// </summary>
    public AdminReportQueryRange Normalize(
        string? preset,
        DateOnly? from,
        DateOnly? to,
        string? granularity,
        string? timezone)
    {
        var normalizedTimezone = string.IsNullOrWhiteSpace(timezone)
            ? DefaultTimezone
            : timezone.Trim();

        var timeZoneInfo = ResolveTimezone(normalizedTimezone);
        var normalizedPreset = string.IsNullOrWhiteSpace(preset)
            ? DefaultPreset
            : preset.Trim().ToUpperInvariant();
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(_dateTimeProvider.UtcNow, timeZoneInfo));

        (DateOnly From, DateOnly To) range = normalizedPreset switch
        {
            "7D" => (localToday.AddDays(-6), localToday),
            "30D" => (localToday.AddDays(-29), localToday),
            "90D" => (localToday.AddDays(-89), localToday),
            "1Y" => (localToday.AddDays(-364), localToday),
            CustomPreset => ResolveCustomRange(from, to),
            _ => throw new BadRequestException($"Unsupported report preset '{normalizedPreset}'.")
        };

        ValidateRange(range.From, range.To);

        var normalizedGranularity = ResolveGranularity(granularity, range.From, range.To);
        var comparisonRange = _comparisonPeriodResolver.Resolve(range.From, range.To);

        return new AdminReportQueryRange(
            normalizedPreset == CustomPreset ? CustomPreset : normalizedPreset,
            range.From,
            range.To,
            normalizedGranularity,
            timeZoneInfo.Id,
            comparisonRange.From,
            comparisonRange.To,
            ToUtcStart(range.From, timeZoneInfo),
            ToUtcEndExclusive(range.To, timeZoneInfo),
            ToUtcStart(comparisonRange.From, timeZoneInfo),
            ToUtcEndExclusive(comparisonRange.To, timeZoneInfo));
    }

    private static (DateOnly From, DateOnly To) ResolveCustomRange(DateOnly? from, DateOnly? to)
    {
        if (!from.HasValue || !to.HasValue)
            throw new BadRequestException("Custom report ranges require both from and to values.");

        return (from.Value, to.Value);
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new BadRequestException("The report from date must be on or before the to date.");

        var dayCount = to.DayNumber - from.DayNumber + 1;
        if (dayCount > MaxRangeDays)
            throw new BadRequestException($"The report range cannot exceed {MaxRangeDays} days.");
    }

    private static ReportGranularity ResolveGranularity(string? granularity, DateOnly from, DateOnly to)
    {
        if (!string.IsNullOrWhiteSpace(granularity))
        {
            return granularity.Trim().ToLowerInvariant() switch
            {
                "day" => ReportGranularity.Day,
                "week" => ReportGranularity.Week,
                "month" => ReportGranularity.Month,
                _ => throw new BadRequestException($"Unsupported report granularity '{granularity}'.")
            };
        }

        var dayCount = to.DayNumber - from.DayNumber + 1;
        if (dayCount <= 31)
            return ReportGranularity.Day;
        if (dayCount <= 120)
            return ReportGranularity.Week;

        return ReportGranularity.Month;
    }

    private static TimeZoneInfo ResolveTimezone(string timezone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BadRequestException($"Unsupported timezone '{timezone}'.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new BadRequestException($"Invalid timezone '{timezone}'.");
        }
    }

    private static DateTime ToUtcStart(DateOnly date, TimeZoneInfo timezone)
    {
        var localDateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timezone);
    }

    private static DateTime ToUtcEndExclusive(DateOnly date, TimeZoneInfo timezone)
    {
        var nextDayLocalDateTime = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(nextDayLocalDateTime, timezone);
    }
}
