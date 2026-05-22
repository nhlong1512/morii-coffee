using FluentAssertions;
using MoriiCoffee.Application.Services.Reports;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Report;

public class ComparisonPeriodResolverTests
{
    [Fact]
    public void Resolve_ReturnsPreviousPeriodWithMatchingInclusiveLength()
    {
        var resolver = new ComparisonPeriodResolver();

        var result = resolver.Resolve(new DateOnly(2026, 05, 01), new DateOnly(2026, 05, 31));

        result.From.Should().Be(new DateOnly(2026, 03, 31));
        result.To.Should().Be(new DateOnly(2026, 04, 30));
    }
}
