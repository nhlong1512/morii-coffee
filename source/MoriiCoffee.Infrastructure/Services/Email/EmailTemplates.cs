using System.Reflection;

namespace MoriiCoffee.Infrastructure.Services.Email;

public static class EmailTemplates
{
    private const string ResourcePrefix = "MoriiCoffee.Infrastructure.Resources.EmailTemplates";

    /// <summary>
    /// Load an email template from embedded resources
    /// </summary>
    /// <param name="templateName">Template filename (e.g., "welcome.html")</param>
    /// <returns>Template content as string</returns>
    /// <exception cref="FileNotFoundException">Thrown if template not found</exception>
    public static string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{ResourcePrefix}.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Email template '{templateName}' not found as embedded resource. " +
                $"Expected resource name: {resourceName}. " +
                $"Ensure the template is marked as an EmbeddedResource in the .csproj file."
            );
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
