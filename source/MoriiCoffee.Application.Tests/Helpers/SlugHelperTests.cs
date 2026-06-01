using FluentAssertions;
using MoriiCoffee.Application.SeedWork.Helpers;
using Xunit;

namespace MoriiCoffee.Application.Tests.Helpers;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Cà phê Việt Nam", "ca-phe-viet-nam")]
    [InlineData("Đậm đà & Dịu nhẹ", "dam-da-diu-nhe")]
    [InlineData("  Coffee Guide  ", "coffee-guide")]
    public void Generate_NormalizesValue(string value, string expected)
    {
        SlugHelper.Generate(value).Should().Be(expected);
    }
}
