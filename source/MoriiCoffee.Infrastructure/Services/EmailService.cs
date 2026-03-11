using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// Stub email service that logs instead of sending real emails.
/// Replace with SMTP or SendGrid implementation in a future phase.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(string to, string name)
    {
        _logger.LogInformation("[EmailService] Sending welcome email to {Name} <{Email}>", name, to);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string token)
    {
        _logger.LogInformation("[EmailService] Sending password reset email to <{Email}>, token: {Token}", to, token);
        return Task.CompletedTask;
    }
}
