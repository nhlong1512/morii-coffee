using FluentValidation;

namespace MoriiCoffee.Application.Commands.Store.ReorderStores;

/// <summary>
/// Validates the bulk store reorder payload.
/// </summary>
public class ReorderStoresCommandValidator : AbstractValidator<ReorderStoresCommand>
{
    public ReorderStoresCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one store reorder item is required.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.Id).Distinct().Count() == items.Count)
            .WithMessage("Store reorder items must not contain duplicate IDs.")
            .When(x => x.Items.Count > 0);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Store ID is required.");

            item.RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Display order must be a non-negative number.");
        });
    }
}
