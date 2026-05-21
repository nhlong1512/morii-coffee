using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Presentation.Controllers;
using System.Reflection;
using Xunit;

namespace MoriiCoffee.Application.Tests.Presentation;

public class BlogAdminAuthorizationTests
{
    [Fact]
    public void AdminBlogPostsController_ClassLevelAuthorization_AllowsAdminAndStaff()
    {
        var attribute = typeof(AdminBlogPostsController).GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be($"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}");
    }

    [Theory]
    [InlineData(nameof(AdminBlogPostsController.CreateBlogPost))]
    [InlineData(nameof(AdminBlogPostsController.UpdateBlogPost))]
    [InlineData(nameof(AdminBlogPostsController.DeleteBlogPost))]
    [InlineData(nameof(AdminBlogPostsController.UpdateBlogPostStatus))]
    public void AdminBlogPostsController_WriteActions_RequireAdminOnly(string methodName)
    {
        var attribute = GetAuthorizeAttribute<AdminBlogPostsController>(methodName);
        attribute.Roles.Should().Be(nameof(ERole.ADMIN));
    }

    [Theory]
    [InlineData(nameof(AdminBlogPostsController.GetAdminBlogPosts))]
    [InlineData(nameof(AdminBlogPostsController.GetAdminBlogPostById))]
    [InlineData(nameof(AdminBlogPostsController.ReorderBlogPosts))]
    public void AdminBlogPostsController_ReadAndReorderActions_DoNotOverrideClassLevelAccess(string methodName)
    {
        var attribute = GetAuthorizeAttributeOrNull<AdminBlogPostsController>(methodName);
        attribute.Should().BeNull();
    }

    [Fact]
    public void AdminBlogCategoriesController_ClassLevelAuthorization_AllowsAdminAndStaff()
    {
        var attribute = typeof(AdminBlogCategoriesController).GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be($"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}");
    }

    [Theory]
    [InlineData(nameof(AdminBlogCategoriesController.CreateBlogCategory))]
    [InlineData(nameof(AdminBlogCategoriesController.UpdateBlogCategory))]
    [InlineData(nameof(AdminBlogCategoriesController.DeleteBlogCategory))]
    public void AdminBlogCategoriesController_WriteActions_RequireAdminOnly(string methodName)
    {
        var attribute = GetAuthorizeAttribute<AdminBlogCategoriesController>(methodName);
        attribute.Roles.Should().Be(nameof(ERole.ADMIN));
    }

    [Theory]
    [InlineData(nameof(AdminBlogCategoriesController.GetAdminBlogCategories))]
    [InlineData(nameof(AdminBlogCategoriesController.ReorderBlogCategories))]
    public void AdminBlogCategoriesController_ReadAndReorderActions_DoNotOverrideClassLevelAccess(string methodName)
    {
        var attribute = GetAuthorizeAttributeOrNull<AdminBlogCategoriesController>(methodName);
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
