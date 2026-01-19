using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CrmSaas.Api.Services;

public interface IDuplicateDetectionService
{
    Task<List<DuplicateGroup>> FindCustomerDuplicatesAsync(Customer? newCustomer = null, CancellationToken cancellationToken = default);
    Task<List<DuplicateGroup>> FindLeadDuplicatesAsync(Lead? newLead = null, CancellationToken cancellationToken = default);
    Task<MergeResult> MergeCustomersAsync(Guid masterId, List<Guid> duplicateIds, CancellationToken cancellationToken = default);
    Task<MergeResult> MergeLeadsAsync(Guid masterId, List<Guid> duplicateIds, CancellationToken cancellationToken = default);
}

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly TenantDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<DuplicateDetectionService> _logger;

    public DuplicateDetectionService(
        TenantDbContext context,
        IAuditService auditService,
        ILogger<DuplicateDetectionService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<DuplicateGroup>> FindCustomerDuplicatesAsync(
        Customer? newCustomer = null, 
        CancellationToken cancellationToken = default)
    {
        var duplicateGroups = new List<DuplicateGroup>();
        
        // Get all customers or filter by new customer
        var customers = newCustomer == null
            ? await _context.Customers.AsNoTracking().ToListAsync(cancellationToken)
            : new List<Customer> { newCustomer };

        foreach (var customer in customers)
        {
            var duplicates = new List<DuplicateMatch>();

            // Rule 1: Exact email match (90% confidence)
            if (!string.IsNullOrEmpty(customer.Email))
            {
                var emailMatches = await _context.Customers
                    .Where(c => c.Id != customer.Id && c.Email == customer.Email)
                    .ToListAsync(cancellationToken);

                duplicates.AddRange(emailMatches.Select(c => new DuplicateMatch
                {
                    RecordId = c.Id,
                    MatchRule = "Email Match",
                    ConfidenceScore = 90,
                    MatchedFields = new[] { "Email" }
                }));
            }

            // Rule 2: Exact phone match (80% confidence)
            if (!string.IsNullOrEmpty(customer.Phone))
            {
                var normalizedPhone = NormalizePhone(customer.Phone);
                var phoneMatches = await _context.Customers
                    .Where(c => c.Id != customer.Id && c.Phone != null)
                    .ToListAsync(cancellationToken);

                phoneMatches = phoneMatches
                    .Where(c => NormalizePhone(c.Phone!) == normalizedPhone)
                    .ToList();

                duplicates.AddRange(phoneMatches.Select(c => new DuplicateMatch
                {
                    RecordId = c.Id,
                    MatchRule = "Phone Match",
                    ConfidenceScore = 80,
                    MatchedFields = new[] { "Phone" }
                }));
            }

            // Rule 3: Tax ID match (95% confidence) - for business customers
            if (customer.Type == CustomerType.Business && !string.IsNullOrEmpty(customer.TaxId))
            {
                var taxIdMatches = await _context.Customers
                    .Where(c => c.Id != customer.Id && 
                               c.Type == CustomerType.Business && 
                               c.TaxId == customer.TaxId)
                    .ToListAsync(cancellationToken);

                duplicates.AddRange(taxIdMatches.Select(c => new DuplicateMatch
                {
                    RecordId = c.Id,
                    MatchRule = "Tax ID Match",
                    ConfidenceScore = 95,
                    MatchedFields = new[] { "TaxId" }
                }));
            }

            // Rule 4: Fuzzy name + address match (70% confidence)
            if (!string.IsNullOrEmpty(customer.Name) && !string.IsNullOrEmpty(customer.AddressLine1))
            {
                var allCustomers = await _context.Customers
                    .Where(c => c.Id != customer.Id)
                    .ToListAsync(cancellationToken);

                foreach (var other in allCustomers)
                {
                    if (string.IsNullOrEmpty(other.Name) || string.IsNullOrEmpty(other.AddressLine1))
                        continue;

                    var nameSimilarity = CalculateSimilarity(customer.Name, other.Name);
                    var addressSimilarity = CalculateSimilarity(customer.AddressLine1, other.AddressLine1);

                    if (nameSimilarity >= 0.85 && addressSimilarity >= 0.85)
                    {
                        duplicates.Add(new DuplicateMatch
                        {
                            RecordId = other.Id,
                            MatchRule = "Name + Address Fuzzy Match",
                            ConfidenceScore = 70,
                            MatchedFields = new[] { "Name", "Address" }
                        });
                    }
                }
            }

            // Group duplicates and deduplicate
            var uniqueDuplicates = duplicates
                .GroupBy(d => d.RecordId)
                .Select(g => new DuplicateMatch
                {
                    RecordId = g.Key,
                    MatchRule = string.Join(", ", g.Select(x => x.MatchRule)),
                    ConfidenceScore = g.Max(x => x.ConfidenceScore),
                    MatchedFields = g.SelectMany(x => x.MatchedFields).Distinct().ToArray()
                })
                .OrderByDescending(d => d.ConfidenceScore)
                .ToList();

            if (uniqueDuplicates.Any())
            {
                duplicateGroups.Add(new DuplicateGroup
                {
                    MasterRecordId = customer.Id,
                    EntityType = "Customer",
                    Duplicates = uniqueDuplicates
                });
            }
        }

        return duplicateGroups;
    }

    public async Task<List<DuplicateGroup>> FindLeadDuplicatesAsync(
        Lead? newLead = null, 
        CancellationToken cancellationToken = default)
    {
        var duplicateGroups = new List<DuplicateGroup>();
        
        var leads = newLead == null
            ? await _context.Leads.AsNoTracking().ToListAsync(cancellationToken)
            : new List<Lead> { newLead };

        foreach (var lead in leads)
        {
            var duplicates = new List<DuplicateMatch>();

            // Rule 1: Exact email match (90% confidence)
            if (!string.IsNullOrEmpty(lead.Email))
            {
                var emailMatches = await _context.Leads
                    .Where(l => l.Id != lead.Id && l.Email == lead.Email)
                    .ToListAsync(cancellationToken);

                duplicates.AddRange(emailMatches.Select(l => new DuplicateMatch
                {
                    RecordId = l.Id,
                    MatchRule = "Email Match",
                    ConfidenceScore = 90,
                    MatchedFields = new[] { "Email" }
                }));
            }

            // Rule 2: Phone match (80% confidence)
            if (!string.IsNullOrEmpty(lead.Phone))
            {
                var normalizedPhone = NormalizePhone(lead.Phone);
                var phoneMatches = await _context.Leads
                    .Where(l => l.Id != lead.Id && l.Phone != null)
                    .ToListAsync(cancellationToken);

                phoneMatches = phoneMatches
                    .Where(l => NormalizePhone(l.Phone!) == normalizedPhone)
                    .ToList();

                duplicates.AddRange(phoneMatches.Select(l => new DuplicateMatch
                {
                    RecordId = l.Id,
                    MatchRule = "Phone Match",
                    ConfidenceScore = 80,
                    MatchedFields = new[] { "Phone" }
                }));
            }

            // Rule 3: Fuzzy name + company match (75% confidence)
            if (!string.IsNullOrEmpty(lead.FirstName) && !string.IsNullOrEmpty(lead.LastName))
            {
                var fullName = $"{lead.FirstName} {lead.LastName}";
                var allLeads = await _context.Leads
                    .Where(l => l.Id != lead.Id)
                    .ToListAsync(cancellationToken);

                foreach (var other in allLeads)
                {
                    var otherName = $"{other.FirstName} {other.LastName}";
                    var nameSimilarity = CalculateSimilarity(fullName, otherName);

                    if (nameSimilarity >= 0.9)
                    {
                        var companyMatch = string.IsNullOrEmpty(lead.CompanyName) || 
                                         string.IsNullOrEmpty(other.CompanyName) ||
                                         CalculateSimilarity(lead.CompanyName, other.CompanyName) >= 0.85;

                        if (companyMatch)
                        {
                            duplicates.Add(new DuplicateMatch
                            {
                                RecordId = other.Id,
                                MatchRule = "Name Match",
                                ConfidenceScore = 75,
                                MatchedFields = new[] { "FirstName", "LastName", "Company" }
                            });
                        }
                    }
                }
            }

            var uniqueDuplicates = duplicates
                .GroupBy(d => d.RecordId)
                .Select(g => new DuplicateMatch
                {
                    RecordId = g.Key,
                    MatchRule = string.Join(", ", g.Select(x => x.MatchRule)),
                    ConfidenceScore = g.Max(x => x.ConfidenceScore),
                    MatchedFields = g.SelectMany(x => x.MatchedFields).Distinct().ToArray()
                })
                .OrderByDescending(d => d.ConfidenceScore)
                .ToList();

            if (uniqueDuplicates.Any())
            {
                duplicateGroups.Add(new DuplicateGroup
                {
                    MasterRecordId = lead.Id,
                    EntityType = "Lead",
                    Duplicates = uniqueDuplicates
                });
            }
        }

        return duplicateGroups;
    }

    public async Task<MergeResult> MergeCustomersAsync(
        Guid masterId, 
        List<Guid> duplicateIds, 
        CancellationToken cancellationToken = default)
    {
        var master = await _context.Customers.FindAsync(new object[] { masterId }, cancellationToken);
        if (master == null)
            throw new InvalidOperationException($"Master customer {masterId} not found");

        var duplicates = await _context.Customers
            .Where(c => duplicateIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (duplicates.Count != duplicateIds.Count)
            throw new InvalidOperationException("Some duplicate customers not found");

        var mergedCount = 0;
        var mergedRecords = new List<Guid>();

        foreach (var duplicate in duplicates)
        {
            // Merge contacts
            var contacts = await _context.Contacts
                .Where(c => c.CustomerId == duplicate.Id)
                .ToListAsync(cancellationToken);
            
            foreach (var contact in contacts)
            {
                contact.CustomerId = masterId;
            }

            // Merge interactions
            var interactions = await _context.Interactions
                .Where(i => i.CustomerId == duplicate.Id)
                .ToListAsync(cancellationToken);
            
            foreach (var interaction in interactions)
            {
                interaction.CustomerId = masterId;
            }

            // Merge opportunities
            var opportunities = await _context.Opportunities
                .Where(o => o.CustomerId == duplicate.Id)
                .ToListAsync(cancellationToken);
            
            foreach (var opportunity in opportunities)
            {
                opportunity.CustomerId = masterId;
            }

            // Merge tickets
            var tickets = await _context.Tickets
                .Where(t => t.CustomerId == duplicate.Id)
                .ToListAsync(cancellationToken);
            
            foreach (var ticket in tickets)
            {
                ticket.CustomerId = masterId;
            }

            // Soft delete duplicate
            duplicate.IsDeleted = true;
            duplicate.DeletedAt = DateTime.UtcNow;

            mergedRecords.Add(duplicate.Id);
            mergedCount++;

            await _auditService.LogAsync(
                AuditActions.Delete, 
                nameof(Customer), 
                duplicate.Id, 
                $"Merged into customer {masterId}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Merged {Count} customers into master {MasterId}",
            mergedCount, masterId);

        return new MergeResult
        {
            MasterRecordId = masterId,
            MergedRecordIds = mergedRecords,
            MergedCount = mergedCount,
            Success = true,
            Message = $"Successfully merged {mergedCount} customers"
        };
    }

    public async Task<MergeResult> MergeLeadsAsync(
        Guid masterId, 
        List<Guid> duplicateIds, 
        CancellationToken cancellationToken = default)
    {
        var master = await _context.Leads.FindAsync(new object[] { masterId }, cancellationToken);
        if (master == null)
            throw new InvalidOperationException($"Master lead {masterId} not found");

        var duplicates = await _context.Leads
            .Where(l => duplicateIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (duplicates.Count != duplicateIds.Count)
            throw new InvalidOperationException("Some duplicate leads not found");

        var mergedCount = 0;
        var mergedRecords = new List<Guid>();

        foreach (var duplicate in duplicates)
        {
            // Merge activities
            var activities = await _context.Activities
                .Where(a => a.LeadId == duplicate.Id)
                .ToListAsync(cancellationToken);
            
            foreach (var activity in activities)
            {
                activity.LeadId = masterId;
            }

            // Soft delete duplicate
            duplicate.IsDeleted = true;
            duplicate.DeletedAt = DateTime.UtcNow;

            mergedRecords.Add(duplicate.Id);
            mergedCount++;

            await _auditService.LogAsync(
                AuditActions.Delete, 
                nameof(Lead), 
                duplicate.Id, 
                $"Merged into lead {masterId}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Merged {Count} leads into master {MasterId}",
            mergedCount, masterId);

        return new MergeResult
        {
            MasterRecordId = masterId,
            MergedRecordIds = mergedRecords,
            MergedCount = mergedCount,
            Success = true,
            Message = $"Successfully merged {mergedCount} leads"
        };
    }

    private string NormalizePhone(string phone)
    {
        // Remove all non-digit characters
        return Regex.Replace(phone, @"[^\d]", "");
    }

    private double CalculateSimilarity(string str1, string str2)
    {
        // Levenshtein distance-based similarity
        str1 = str1.ToLowerInvariant().Trim();
        str2 = str2.ToLowerInvariant().Trim();

        if (str1 == str2) return 1.0;

        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        
        return 1.0 - ((double)distance / maxLength);
    }

    private int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++)
            d[i, 0] = i;
        for (var j = 0; j <= m; j++)
            d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}

public class DuplicateGroup
{
    public Guid MasterRecordId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public List<DuplicateMatch> Duplicates { get; set; } = new();
}

public class DuplicateMatch
{
    public Guid RecordId { get; set; }
    public string MatchRule { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public string[] MatchedFields { get; set; } = Array.Empty<string>();
}

public class MergeResult
{
    public Guid MasterRecordId { get; set; }
    public List<Guid> MergedRecordIds { get; set; } = new();
    public int MergedCount { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
