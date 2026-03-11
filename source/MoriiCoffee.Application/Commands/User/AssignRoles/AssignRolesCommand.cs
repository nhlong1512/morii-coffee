using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.User.AssignRoles;

/// <summary>Command to atomically replace a user's full role set. Role changes take effect on the user's next sign-in.</summary>
public class AssignRolesCommand : ICommand<bool>
{
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
}
