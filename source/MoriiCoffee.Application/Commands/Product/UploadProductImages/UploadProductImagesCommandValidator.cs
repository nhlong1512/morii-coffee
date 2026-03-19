using FluentValidation;

namespace MoriiCoffee.Application.Commands.Product.UploadProductImages;

/// <summary>Validates file type, size, and count constraints before the upload handler runs.</summary>
public class UploadProductImagesCommandValidator : AbstractValidator<UploadProductImagesCommand>
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadProductImagesCommandValidator()
    {
        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("At least one image file is required.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("Each image must not exceed 5 MB.");

            file.RuleFor(f => Path.GetExtension(f.FileName))
                .Must(ext => AllowedExtensions.Contains(ext))
                .WithMessage("Only jpg, jpeg, png, and webp images are allowed.");
        });
    }
}
