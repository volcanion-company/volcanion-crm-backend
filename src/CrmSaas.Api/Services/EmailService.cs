using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CrmSaas.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
    Task SendEmailAsync(List<string> to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
    Task SendEmailWithAttachmentsAsync(string to, string subject, string body, List<EmailAttachment> attachments, bool isHtml = false, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(new List<string> { to }, subject, body, isHtml, cancellationToken);
    }

    public async Task SendEmailAsync(List<string> to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        await SendEmailWithAttachmentsAsync(to, subject, body, new List<EmailAttachment>(), isHtml, cancellationToken);
    }

    public async Task SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        List<EmailAttachment> attachments,
        bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        await SendEmailWithAttachmentsAsync(new List<string> { to }, subject, body, attachments, isHtml, cancellationToken);
    }

    private async Task SendEmailWithAttachmentsAsync(
        List<string> to,
        string subject,
        string body,
        List<EmailAttachment> attachments,
        bool isHtml,
        CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Email sending is disabled. Email to {Recipients} not sent", string.Join(", ", to));
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var recipient in to)
            {
                message.To.Add(recipient);
            }

            // Add attachments
            foreach (var attachment in attachments)
            {
                var mailAttachment = new Attachment(new MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType);
                message.Attachments.Add(mailAttachment);
            }

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl
            };

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipients}: {Subject}",
                string.Join(", ", to), subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}: {Subject}",
                string.Join(", ", to), subject);
            throw;
        }
    }
}

#region Settings & Models

public class EmailSettings
{
    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

#endregion
