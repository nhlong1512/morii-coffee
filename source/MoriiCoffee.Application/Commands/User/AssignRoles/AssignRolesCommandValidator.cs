using FluentValidation;
using MoriiCoffee.Domain.Shared.Enums.User;

namespace MoriiCoffee.Application.Commands.User.AssignRoles;

/// <summary>Validates AssignRolesCommand: Roles list must be non-empty and each entry must be one of ADMIN, STAFF, or CUSTOMER.</summary>
public class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    private static readonly string[] ValidRoles = Enum.GetNames<ERole>();

    public AssignRolesCommandValidator()
    {
        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role must be specified.");

        RuleForEach(x => x.Roles)
            .Must(r => ValidRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Each role must be one of: {string.Join(", ", ValidRoles)}.");
    }
}
