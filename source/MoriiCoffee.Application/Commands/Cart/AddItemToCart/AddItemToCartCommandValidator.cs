using FluentValidation;

namespace MoriiCoffee.Application.Commands.Cart.AddItemToCart;

/// <summary>Validates <see cref="AddItemToCartCommand"/> before the handler executes.</summary>
public class AddItemToCartCommandValidator : AbstractValidator<AddItemToCartCommand>
{
    /// <summary>Configures validation rules for adding an item to the cart.</summary>
    public AddItemToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}
