using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Presentation.Controllers;
using System.Reflection;
using Xunit;

namespace MoriiCoffee.Application.Tests.Presentation;

public class AdminStoresAuthorizationTests
{
    [Fact]
    public void AdminStoresController_ClassLevelAuthorization_AllowsAdminAndStaff()
    {
        var attribute = typeof(AdminStoresController).GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be($"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}");
    }

    [Theory]
    [InlineData(nameof(AdminStoresController.CreateStore))]
    [InlineData(nameof(AdminStoresController.UpdateStore))]
    [InlineData(nameof(AdminStoresController.DeleteStore))]
    [InlineData(nameof(AdminStoresController.UpdateStoreStatus))]
    public void AdminStoresController_WriteActions_RequireAdminOnly(string methodName)
    {
        var attribute = GetAuthorizeAttribute<AdminStoresController>(methodName);
        attribute.Roles.Should().Be(nameof(ERole.ADMIN));
    }

    [Theory]
    [InlineData(nameof(AdminStoresController.GetAdminStores))]
    [InlineData(nameof(AdminStoresController.GetAdminStoreById))]
    [InlineData(nameof(AdminStoresController.ReorderStores))]
    public void AdminStoresController_ReadAndReorderActions_DoNotOverrideClassLevelAccess(string methodName)
    {
        var attribute = GetAuthorizeAttributeOrNull<AdminStoresController>(methodName);
        attribute.Should().BeNull();
    }

    private static AuthorizeAttribute GetAuthorizeAttribute<TController>(string methodName)
    {
        return GetAuthorizeAttributeOrNull<TController>(methodName)
            ?? throw new Xunit.Sdk.XunitException($"Expected [Authorize] on {typeof(TController).Name}.{methodName}");
    }

    private static AuthorizeAttribute? GetAuthorizeAttributeOrNull<TController>(string methodName)
    {
        return typeof(TController)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!
            .GetCustomAttribute<AuthorizeAttribute>();
    }
}
