using FluentValidation;

namespace MoriiCoffee.Application.Commands.Cart.UpdateCartItemQuantity;

/// <summary>Validates <see cref="UpdateCartItemQuantityCommand"/> before the handler executes.</summary>
public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    /// <summary>Configures validation rules for updating a cart item's quantity.</summary>
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be 0 or greater.");
    }
}
