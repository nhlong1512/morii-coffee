using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Shared.Settings;
using Task = System.Threading.Tasks.Task;

namespace MoriiCoffee.Infrastructure.Services.Email;

public class BrevoEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly TransactionalEmailsApi _apiInstance;

    public BrevoEmailService(
        EmailSettings emailSettings,
        UserManager<User> userManager,
        ILogger<BrevoEmailService> logger)
    {
        _emailSettings = emailSettings ?? throw new ArgumentNullException(nameof(emailSettings));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure Brevo SDK
        brevo_csharp.Client.Configuration.Default.ApiKey["api-key"] = _emailSettings.Brevo.ApiKey;
        _apiInstance = new TransactionalEmailsApi();
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName)
    {
        var template = EmailTemplates.LoadTemplate("welcome.html");
        var htmlContent = template
            .Replace("{{UserName}}", toName)
            .Replace("{{StorefrontUrl}}", _emailSettings.StorefrontUrl);

        var subject = $"Welcome to Morii Coffee, {toName}!";

        await SendAsync(toEmail, toName, subject, htmlContent);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
    {
        // Lookup user display name for personalization
        var user = await _userManager.FindByEmailAsync(toEmail);
        var displayName = user?.FullName ?? user?.UserName ?? "there";

        var template = EmailTemplates.LoadTemplate("password-reset.html");
        var htmlContent = template
            .Replace("{{UserName}}", displayName)
            .Replace("{{ResetUrl}}", resetUrl);

        var subject = "Reset Your Morii Coffee Password";

        await SendAsync(toEmail, displayName, subject, htmlContent);
    }

    /// <summary>
    /// Core email sending logic using Brevo API
    /// </summary>
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        try
        {
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_emailSettings.FromName, _emailSettings.FromEmail),
                To = new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(toEmail, toName)
                },
                Subject = subject,
                HtmlContent = htmlContent
            };

            var result = await _apiInstance.SendTransacEmailAsync(sendSmtpEmail);

            _logger.LogInformation(
                "[BrevoEmailService] Email '{Subject}' sent to {Email}. MessageId: {MessageId}",
                subject,
                toEmail,
                result.MessageId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[BrevoEmailService] Failed to send email to {Email}. Subject: '{Subject}'",
                toEmail,
                subject
            );
        }
    }
}
