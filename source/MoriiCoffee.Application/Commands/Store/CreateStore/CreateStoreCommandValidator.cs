using FluentValidation;

namespace MoriiCoffee.Application.Commands.Store.CreateStore;

/// <summary>FluentValidation validator for <see cref="CreateStoreCommand"/>.</summary>
public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    private const string TimePattern = "^([01]\\d|2[0-3]):[0-5]\\d$";

    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Slug));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidAbsoluteUri)
            .WithMessage("CoverImageUrl must be a valid absolute URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl));
        RuleFor(x => x.OpeningHours)
            .NotEmpty()
            .Must(h => h.Count == 7).WithMessage("OpeningHours must contain exactly 7 items.")
            .Must(h => h.Select(i => i.DayOfWeek).Distinct().Count() == 7)
            .WithMessage("Each day of week (0–6) must appear exactly once in OpeningHours.");
        RuleForEach(x => x.OpeningHours).ChildRules(h =>
        {
            h.RuleFor(i => i.DayOfWeek).InclusiveBetween(0, 6);
            h.RuleFor(i => i.OpenTime).Matches(TimePattern).WithMessage("OpenTime must be in HH:mm format.");
            h.RuleFor(i => i.CloseTime).Matches(TimePattern).WithMessage("CloseTime must be in HH:mm format.");
            h.RuleFor(i => i)
                .Must(hours => hours.IsClosed || string.CompareOrdinal(hours.OpenTime, hours.CloseTime) < 0)
                .WithMessage("OpenTime must be earlier than CloseTime for open days.");
        });
    }

    private static bool BeValidAbsoluteUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out _);
}
