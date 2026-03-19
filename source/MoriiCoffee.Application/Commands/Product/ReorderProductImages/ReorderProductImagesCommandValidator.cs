using FluentValidation;

namespace MoriiCoffee.Application.Commands.Product.ReorderProductImages;

/// <summary>Ensures the reorder list is non-empty and contains no duplicates.</summary>
public class ReorderProductImagesCommandValidator : AbstractValidator<ReorderProductImagesCommand>
{
    public ReorderProductImagesCommandValidator()
    {
        RuleFor(x => x.ImageIds)
            .NotEmpty().WithMessage("At least one image ID is required.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Image IDs must be unique — no duplicates allowed.");
    }
}
