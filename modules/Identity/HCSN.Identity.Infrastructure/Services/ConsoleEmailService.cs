using System;
using System.Threading.Tasks;
using HCSN.Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HCSN.Identity.Infrastructure.Services;

// In your concrete email service class
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Your email sending logic (SendGrid, SMTP, etc.)
        _logger.LogInformation("Sending email to {To}: {Subject}", to, subject);
        await Task.CompletedTask;
    }

    public async Task SendEmailWithTemplateAsync(
        string to,
        string templateId,
        Dictionary<string, object> templateData
    )
    {
        // Render template and send
        var subject = GetSubjectFromTemplate(templateId);
        var body = RenderTemplate(templateId, templateData);
        await SendEmailAsync(to, subject, body);
    }

    private string GetSubjectFromTemplate(string templateId)
    {
        return templateId switch
        {
            "confirm-email" => "Confirm Your Email",
            "welcome-email" => "Welcome!",
            _ => "Notification",
        };
    }

    private string RenderTemplate(string templateId, Dictionary<string, object> data)
    {
        // Simple template rendering - in reality, use a proper template engine
        return templateId switch
        {
            "confirm-email" => $@"
                <h1>Hello {data["user_name"]}</h1>
                <p>Please confirm your email by clicking: <a href='{data["confirmation_url"]}'>here</a></p>
                <p>This link expires in {data["expiry_hours"]} hours.</p>",

            "welcome-email" => $@"
                <h1>Welcome to {data["tenant_name"]}!</h1>
                <p>Your account has been created. <a href='{data["login_url"]}'>Login here</a></p>
                <p>Support: {data["support_email"]}</p>",

            _ => "No template found",
        };
    }
}
