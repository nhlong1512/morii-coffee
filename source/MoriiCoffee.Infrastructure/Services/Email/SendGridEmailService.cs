using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Settings;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Infrastructure.Services.Email;

/// <summary>
/// Production email service backed by SendGrid.
/// Sends branded HTML emails for welcome and password-reset scenarios.
/// Registered when <c>EmailSettings:Provider</c> is <c>"SendGrid"</c>.
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly EmailAddress _from;
    private readonly EmailSettings _settings;
    private readonly UserManager<UserEntity> _userManager;
    private readonly ILogger _logger;

    /// <summary>Initialises the SendGrid client with the configured API key.</summary>
    public SendGridEmailService(
        EmailSettings settings,
        UserManager<UserEntity> userManager,
        ILogger logger)
    {
        _settings = settings;
        _userManager = userManager;
        _logger = logger;
        _client = new SendGridClient(settings.SendGrid.ApiKey);
        _from = new EmailAddress(settings.FromEmail, settings.FromName);
    }

    /// <summary>
    /// Sends a branded welcome email to the newly registered user.
    /// Resolves the user's display name from the database.
    /// </summary>
    public async Task SendWelcomeEmailAsync(string to, string name)
    {
        var subject = $"Welcome to Morii Coffee, {name}!";
        var html = EmailTemplates.WelcomeEmail(name, _settings.StorefrontUrl);

        await SendAsync(to, name, subject, html);
    }

    /// <summary>
    /// Sends a password-reset email containing a secure link.
    /// The Identity-generated token is URL-encoded and appended to the configured base URL.
    /// </summary>
    public async Task SendPasswordResetEmailAsync(string to, string token)
    {
        var user = await _userManager.FindByEmailAsync(to);
        var displayName = user?.FullName ?? user?.UserName ?? "there";

        var encodedToken = Uri.EscapeDataString(token);
        var resetUrl = $"{_settings.ResetPasswordBaseUrl}?token={encodedToken}&email={Uri.EscapeDataString(to)}";

        var subject = "Reset Your Morii Coffee Password";
        var html = EmailTemplates.PasswordResetEmail(displayName, resetUrl);

        await SendAsync(to, displayName, subject, html);
    }

    /// <summary>Internal helper that builds and dispatches the SendGrid message. Logs failures without throwing.</summary>
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var to = new EmailAddress(toEmail, toName);
        var msg = MailHelper.CreateSingleEmail(_from, to, subject, plainTextContent: null, htmlBody);

        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.Error(
                "[SendGridEmailService] Failed to send email to {Email}. Status: {Status}. Body: {Body}",
                toEmail, (int)response.StatusCode, body);
        }
        else
        {
            _logger.Information(
                "[SendGridEmailService] Email '{Subject}' sent to {Email}",
                subject, toEmail);
        }
    }
}
