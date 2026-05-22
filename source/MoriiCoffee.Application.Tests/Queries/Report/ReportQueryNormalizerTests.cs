using FluentAssertions;
using Moq;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Reports;
using MoriiCoffee.Domain.Repositories;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Report;

public class ReportQueryNormalizerTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly ReportQueryNormalizer _normalizer;

    public ReportQueryNormalizerTests()
    {
        _dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTime(2026, 05, 22, 14, 0, 0, DateTimeKind.Utc));
        _normalizer = new ReportQueryNormalizer(_dateTimeProvider.Object, new ComparisonPeriodResolver());
    }

    [Fact]
    public void Normalize_DefaultPreset_UsesLocalTodayAndDailyGranularity()
    {
        var result = _normalizer.Normalize(null, null, null, null, "Asia/Ho_Chi_Minh");

        result.Preset.Should().Be("30D");
        result.From.Should().Be(new DateOnly(2026, 04, 23));
        result.To.Should().Be(new DateOnly(2026, 05, 22));
        result.Granularity.Should().Be(ReportGranularity.Day);
        result.ComparisonFrom.Should().Be(new DateOnly(2026, 03, 24));
        result.ComparisonTo.Should().Be(new DateOnly(2026, 04, 22));
    }

    [Fact]
    public void Normalize_CustomRange_InfersWeeklyGranularityForMediumRanges()
    {
        var result = _normalizer.Normalize("CUSTOM", new DateOnly(2026, 01, 01), new DateOnly(2026, 03, 15), null, "Asia/Ho_Chi_Minh");

        result.Granularity.Should().Be(ReportGranularity.Week);
    }

    [Fact]
    public void Normalize_ExplicitGranularity_OverridesInference()
    {
        var result = _normalizer.Normalize("7D", null, null, "month", "Asia/Ho_Chi_Minh");

        result.Granularity.Should().Be(ReportGranularity.Month);
    }

    [Fact]
    public void Normalize_InvalidTimezone_ThrowsBadRequest()
    {
        var act = () => _normalizer.Normalize("7D", null, null, null, "Mars/Phobos");

        act.Should().Throw<BadRequestException>()
            .WithMessage("*Unsupported timezone*");
    }

    [Fact]
    public void Normalize_CustomRangeBeyondLimit_ThrowsBadRequest()
    {
        var act = () => _normalizer.Normalize("CUSTOM", new DateOnly(2025, 01, 01), new DateOnly(2026, 01, 02), null, "Asia/Ho_Chi_Minh");

        act.Should().Throw<BadRequestException>()
            .WithMessage("*cannot exceed 366 days*");
    }
}
