using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.CreatePaymentIntent;

/// <summary>Validates CreatePaymentIntentCommand: amount must be positive, currency required.</summary>
public class CreatePaymentIntentCommandValidator : AbstractValidator<CreatePaymentIntentCommand>
{
    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "vnd", "usd", "eur", "sgd" };

    public CreatePaymentIntentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(c => SupportedCurrencies.Contains(c))
            .WithMessage("Unsupported currency. Supported: vnd, usd, eur, sgd.");
    }
}
