using FluentValidation;

namespace MoriiCoffee.Application.Commands.Banner.CreateBanner;

/// <summary>Validates <see cref="CreateBannerCommand"/> before the handler executes.</summary>
public class CreateBannerCommandValidator : AbstractValidator<CreateBannerCommand>
{
    public CreateBannerCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Banner title is required.")
            .MaximumLength(200).WithMessage("Banner title must not exceed 200 characters.");

        RuleFor(x => x.Subtitle)
            .MaximumLength(500).WithMessage("Subtitle must not exceed 500 characters.")
            .When(x => x.Subtitle is not null);

        RuleFor(x => x.Cta)
            .MaximumLength(100).WithMessage("CTA label must not exceed 100 characters.")
            .When(x => x.Cta is not null);

        RuleFor(x => x.CtaLink)
            .MaximumLength(500).WithMessage("CTA link must not exceed 500 characters.")
            .When(x => x.CtaLink is not null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
