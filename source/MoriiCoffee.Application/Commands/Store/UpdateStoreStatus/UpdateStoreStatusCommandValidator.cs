using FluentValidation;

namespace MoriiCoffee.Application.Commands.Store.UpdateStoreStatus;

/// <summary>
/// Validates the store status update payload.
/// </summary>
public class UpdateStoreStatusCommandValidator : AbstractValidator<UpdateStoreStatusCommand>
{
    public UpdateStoreStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Store ID is required.");
    }
}
