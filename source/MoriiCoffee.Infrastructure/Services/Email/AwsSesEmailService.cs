using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Settings;
using Serilog;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Infrastructure.Services.Email;

/// <summary>
/// Production email service backed by AWS Simple Email Service (SES v1).
/// Sends branded HTML emails for welcome and password-reset scenarios.
/// Registered when <c>EmailSettings:Provider</c> is <c>"AwsSes"</c>.
/// </summary>
public class AwsSesEmailService : IEmailService
{
    private readonly AmazonSimpleEmailServiceClient _client;
    private readonly EmailSettings _settings;
    private readonly UserManager<UserEntity> _userManager;
    private readonly ILogger _logger;

    /// <summary>Initialises the SES client using explicit credentials from configuration.</summary>
    public AwsSesEmailService(
        EmailSettings settings,
        UserManager<UserEntity> userManager,
        ILogger logger)
    {
        _settings = settings;
        _userManager = userManager;
        _logger = logger;

        var credentials = new BasicAWSCredentials(
            settings.AwsSes.AccessKey,
            settings.AwsSes.SecretKey);

        var region = RegionEndpoint.GetBySystemName(settings.AwsSes.Region);
        _client = new AmazonSimpleEmailServiceClient(credentials, region);
    }

    /// <summary>
    /// Sends a branded welcome email to the newly registered user.
    /// Resolves the user's display name from the database.
    /// </summary>
    public async Task SendWelcomeEmailAsync(string to, string name)
    {
        var subject = $"Welcome to Morii Coffee, {name}!";
        var html = EmailTemplates.WelcomeEmail(name);

        await SendAsync(to, subject, html);
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

        await SendAsync(to, subject, html);
    }

    /// <summary>Internal helper that builds and dispatches the SES SendEmail request. Logs failures without throwing.</summary>
    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var request = new SendEmailRequest
        {
            Source = $"{_settings.FromName} <{_settings.FromEmail}>",
            Destination = new Destination
            {
                ToAddresses = new List<string> { toEmail }
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = htmlBody
                    }
                }
            }
        };

        try
        {
            await _client.SendEmailAsync(request);
            _logger.Information(
                "[AwsSesEmailService] Email '{Subject}' sent to {Email}",
                subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "[AwsSesEmailService] Failed to send email to {Email}. Subject: {Subject}",
                toEmail, subject);
        }
    }
}
