using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Presentation.Controllers;
using System.Reflection;
using Xunit;

namespace MoriiCoffee.Application.Tests.Presentation;

public class AdminReportsAuthorizationTests
{
    [Fact]
    public void AdminReportsController_ClassLevelAuthorization_RequiresAdmin()
    {
        var attribute = typeof(AdminReportsController).GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be(nameof(ERole.ADMIN));
    }

    [Theory]
    [InlineData(nameof(AdminReportsController.GetDashboard))]
    [InlineData(nameof(AdminReportsController.Export))]
    public void AdminReportsController_Actions_DoNotOverrideClassLevelAuthorization(string methodName)
    {
        var attribute = typeof(AdminReportsController)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().BeNull();
    }
}
