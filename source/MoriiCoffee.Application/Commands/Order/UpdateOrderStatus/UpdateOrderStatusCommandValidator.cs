using FluentValidation;

namespace MoriiCoffee.Application.Commands.Order.UpdateOrderStatus;

/// <summary>Validates <see cref="UpdateOrderStatusCommand"/> before the handler executes.</summary>
public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    /// <summary>Configures validation rules for updating an order status.</summary>
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("A valid order status is required.");
    }
}
