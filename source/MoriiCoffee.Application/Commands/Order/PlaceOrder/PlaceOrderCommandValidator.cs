using FluentValidation;

namespace MoriiCoffee.Application.Commands.Order.PlaceOrder;

/// <summary>Validates <see cref="PlaceOrderCommand"/> before the handler executes.</summary>
public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    /// <summary>Configures validation rules for placing an order.</summary>
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(15).WithMessage("Phone number must not exceed 15 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment method is required.");
    }
}
