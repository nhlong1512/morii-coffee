using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Presentation.Controllers;
using Xunit;

namespace MoriiCoffee.Application.Tests.Presentation;

public class ShippingAuthorizationTests
{
    [Theory]
    [InlineData(nameof(ShippingController.GetProvinces))]
    [InlineData(nameof(ShippingController.GetDistricts))]
    [InlineData(nameof(ShippingController.GetWards))]
    public void ShippingController_MasterDataEndpoints_AllowAnonymous(string methodName)
    {
        var attribute = GetAllowAnonymousAttribute<ShippingController>(methodName);
        attribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(ShippingController.CreateQuote))]
    [InlineData(nameof(ShippingController.GetShipmentByOrderId))]
    public void ShippingController_CustomerEndpoints_RequireAuthenticatedUser(string methodName)
    {
        var authorize = GetAuthorizeAttributeOrNull<ShippingController>(methodName);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().BeNull();
    }

    [Theory]
    [InlineData(nameof(ShippingController.CreateShipment))]
    [InlineData(nameof(ShippingController.RequoteShipment))]
    [InlineData(nameof(ShippingController.SyncShipment))]
    [InlineData(nameof(ShippingController.CancelShipment))]
    [InlineData(nameof(ShippingController.UpdateShipmentNote))]
    public void ShippingController_AdminActions_RequireAdminRole(string methodName)
    {
        var authorize = GetAuthorizeAttribute<ShippingController>(methodName);
        authorize.Roles.Should().Be(nameof(ERole.ADMIN));
    }

    [Fact]
    public void ShippingWebhookController_ClassLevel_AllowsAnonymous()
    {
        var allowAnonymous = typeof(ShippingWebhookController).GetCustomAttribute<AllowAnonymousAttribute>();
        allowAnonymous.Should().NotBeNull();
    }

    [Fact]
    public void ShippingWebhookController_Receive_DoesNotRequireAuthorizeOverride()
    {
        var authorize = GetAuthorizeAttributeOrNull<ShippingWebhookController>(nameof(ShippingWebhookController.Receive));
        authorize.Should().BeNull();
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

    private static AllowAnonymousAttribute? GetAllowAnonymousAttribute<TController>(string methodName)
    {
        return typeof(TController)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!
            .GetCustomAttribute<AllowAnonymousAttribute>();
    }
}
