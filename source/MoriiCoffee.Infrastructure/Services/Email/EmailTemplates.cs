using System.Reflection;

namespace MoriiCoffee.Infrastructure.Services.Email;

/// <summary>
/// Loads branded HTML email templates from embedded resources and injects dynamic values.
/// Template files live in <c>Resources/EmailTemplates/</c> and are compiled into the assembly.
/// Placeholders use the <c>{{KEY}}</c> convention.
/// </summary>
internal static class EmailTemplates
{
    private static readonly Assembly _assembly = typeof(EmailTemplates).Assembly;

    /// <summary>Generates the welcome email HTML, substituting the user's display name.</summary>
    /// <param name="name">The user's display name shown in the greeting.</param>
    public static string WelcomeEmail(string name)
    {
        return LoadTemplate("welcome.html")
            .Replace("{{NAME}}", HtmlEncode(name));
    }

    /// <summary>
    /// Generates the password-reset email HTML, substituting the user's display name
    /// and the fully-formed reset URL.
    /// </summary>
    /// <param name="name">The user's display name shown in the greeting.</param>
    /// <param name="resetUrl">Complete reset URL including the token query parameter.</param>
    public static string PasswordResetEmail(string name, string resetUrl)
    {
        return LoadTemplate("password-reset.html")
            .Replace("{{NAME}}", HtmlEncode(name))
            .Replace("{{RESET_URL}}", resetUrl);
    }

    /// <summary>
    /// Reads an embedded HTML template file by filename and returns its content as a string.
    /// Throws <see cref="InvalidOperationException"/> if the resource is not found.
    /// </summary>
    private static string LoadTemplate(string fileName)
    {
        var resourceName = $"MoriiCoffee.Infrastructure.Resources.EmailTemplates.{fileName}";

        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Email template '{fileName}' not found. Expected resource: '{resourceName}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Minimal HTML-encode to prevent XSS when embedding user-supplied values in templates.</summary>
    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
