using Microsoft.AspNetCore.Identity;
using Moq;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Tests.Helpers;

internal static class UserManagerHelper
{
    internal static Mock<UserManager<UserEntity>> Create()
    {
        var store = new Mock<IUserStore<UserEntity>>();
        return new Mock<UserManager<UserEntity>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
