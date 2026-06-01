using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MoriiCoffee.Application.SeedWork.Helpers;

public static class SlugHelper
{
    public static string Generate(string value)
    {
        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormD);

        var withoutDiacritics = string.Concat(normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));

        var safeCharacters = Regex.Replace(withoutDiacritics, @"[^a-z0-9\s-]", string.Empty);

        return Regex.Replace(safeCharacters, @"[\s-]+", "-").Trim('-');
    }
}
