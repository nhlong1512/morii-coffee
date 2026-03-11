using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.User.AssignRoles;

/// <summary>Removes all current roles then adds the requested roles atomically via UserManager.</summary>
public class AssignRolesCommandHandler : ICommandHandler<AssignRolesCommand, bool>
{
    private readonly UserManager<UserEntity> _userManager;

    public AssignRolesCommandHandler(UserManager<UserEntity> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> Handle(AssignRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException("User", request.UserId);

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, request.Roles.Select(r => r.ToUpperInvariant()));

        return true;
    }
}
