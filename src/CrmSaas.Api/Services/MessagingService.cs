using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CrmSaas.Api.Services;

public interface IMessagingService
{
    Task<MessageResult> SendCampaignEmailAsync(Guid campaignId, Guid recipientId, string recipientType, CancellationToken cancellationToken = default);
    Task<BatchMessageResult> SendBatchCampaignEmailsAsync(Guid campaignId, List<Guid> recipientIds, string recipientType, CancellationToken cancellationToken = default);
    Task<MessageResult> SendTemplatedEmailAsync(string templateName, string toEmail, Dictionary<string, string> placeholders, CancellationToken cancellationToken = default);
}

public class MessagingService : IMessagingService
{
    private readonly TenantDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(
        TenantDbContext context,
        IEmailService emailService,
        ILogger<MessagingService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<MessageResult> SendCampaignEmailAsync(
        Guid campaignId, 
        Guid recipientId, 
        string recipientType,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
        {
            return new MessageResult
            {
                Success = false,
                ErrorMessage = $"Campaign {campaignId} not found"
            };
        }

        // Get recipient email
        var (email, name) = await GetRecipientInfoAsync(recipientId, recipientType, cancellationToken);
        if (string.IsNullOrEmpty(email))
        {
            return new MessageResult
            {
                Success = false,
                ErrorMessage = $"No email found for {recipientType} {recipientId}"
            };
        }

        // Get email template from campaign's templates collection
        var template = await _context.CommunicationTemplates
            .Where(t => t.CampaignId == campaignId && t.Type == CommunicationType.Email)
            .FirstOrDefaultAsync(cancellationToken);

        string subject = campaign.Name;
        string body = campaign.Description ?? string.Empty;

        if (template != null)
        {
            subject = template.Subject ?? campaign.Name;
            body = template.Body ?? campaign.Description ?? string.Empty;
        }

        // Replace placeholders
        var placeholders = new Dictionary<string, string>
        {
            { "RecipientName", name },
            { "CampaignName", campaign.Name },
            { "UnsubscribeLink", $"https://example.com/unsubscribe/{recipientId}" }
        };

        subject = ReplacePlaceholders(subject, placeholders);
        body = ReplacePlaceholders(body, placeholders);

        try
        {
            await _emailService.SendEmailAsync(email, subject, body, isHtml: true, cancellationToken);

            // Track campaign member
            var campaignMember = await _context.CampaignMembers
                .FirstOrDefaultAsync(cm => cm.CampaignId == campaignId && 
                                         (cm.LeadId == recipientId || cm.ContactId == recipientId),
                                   cancellationToken);

            if (campaignMember != null)
            {
                campaignMember.Status = CampaignMemberStatus.Sent;
                campaignMember.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new MessageResult
            {
                Success = true,
                RecipientEmail = email,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending campaign email to {Email}", email);
            return new MessageResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                RecipientEmail = email
            };
        }
    }

    public async Task<BatchMessageResult> SendBatchCampaignEmailsAsync(
        Guid campaignId, 
        List<Guid> recipientIds, 
        string recipientType,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MessageResult>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var recipientId in recipientIds)
        {
            var result = await SendCampaignEmailAsync(campaignId, recipientId, recipientType, cancellationToken);
            results.Add(result);

            if (result.Success)
                successCount++;
            else
                failureCount++;

            // Rate limiting - wait 100ms between emails
            await Task.Delay(100, cancellationToken);
        }

        return new BatchMessageResult
        {
            TotalCount = recipientIds.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results,
            CompletedAt = DateTime.UtcNow
        };
    }

    public async Task<MessageResult> SendTemplatedEmailAsync(
        string templateName, 
        string toEmail, 
        Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName && t.Type == CommunicationType.Email, 
                               cancellationToken);

        if (template == null)
        {
            return new MessageResult
            {
                Success = false,
                ErrorMessage = $"Template '{templateName}' not found"
            };
        }

        var subject = ReplacePlaceholders(template.Subject ?? templateName, placeholders);
        var body = ReplacePlaceholders(template.Body ?? string.Empty, placeholders);

        try
        {
            await _emailService.SendEmailAsync(toEmail, subject, body, isHtml: true, cancellationToken);

            return new MessageResult
            {
                Success = true,
                RecipientEmail = toEmail,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending templated email to {Email}", toEmail);
            return new MessageResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                RecipientEmail = toEmail
            };
        }
    }

    private async Task<(string email, string name)> GetRecipientInfoAsync(
        Guid recipientId, 
        string recipientType,
        CancellationToken cancellationToken)
    {
        return recipientType.ToLowerInvariant() switch
        {
            "customer" => await GetCustomerInfoAsync(recipientId, cancellationToken),
            "lead" => await GetLeadInfoAsync(recipientId, cancellationToken),
            "contact" => await GetContactInfoAsync(recipientId, cancellationToken),
            _ => (string.Empty, string.Empty)
        };
    }

    private async Task<(string email, string name)> GetCustomerInfoAsync(
        Guid customerId, 
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        return customer != null 
            ? (customer.Email ?? string.Empty, customer.Name) 
            : (string.Empty, string.Empty);
    }

    private async Task<(string email, string name)> GetLeadInfoAsync(
        Guid leadId, 
        CancellationToken cancellationToken)
    {
        var lead = await _context.Leads
            .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);

        return lead != null 
            ? (lead.Email ?? string.Empty, $"{lead.FirstName} {lead.LastName}") 
            : (string.Empty, string.Empty);
    }

    private async Task<(string email, string name)> GetContactInfoAsync(
        Guid contactId, 
        CancellationToken cancellationToken)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);

        return contact != null 
            ? (contact.Email ?? string.Empty, $"{contact.FirstName} {contact.LastName}") 
            : (string.Empty, string.Empty);
    }

    private string ReplacePlaceholders(string text, Dictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        foreach (var placeholder in placeholders)
        {
            // Support both {{Key}} and {Key} formats
            text = Regex.Replace(text, 
                $@"\{{\{{?\s*{placeholder.Key}\s*\}}?\}}", 
                placeholder.Value, 
                RegexOptions.IgnoreCase);
        }

        return text;
    }
}

public class MessageResult
{
    public bool Success { get; set; }
    public string? RecipientEmail { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BatchMessageResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<MessageResult> Results { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}
