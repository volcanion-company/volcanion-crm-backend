using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace CrmSaas.Api.Services;

public interface ISegmentationService
{
    Task<List<Guid>> CalculateSegmentMembersAsync(Guid segmentId, CancellationToken cancellationToken = default);
    Task<List<Guid>> EvaluateSegmentFiltersAsync(SegmentCriteria criteria, string entityType, CancellationToken cancellationToken = default);
    Task UpdateSegmentMemberCountAsync(Guid segmentId, CancellationToken cancellationToken = default);
    Task<SegmentMembershipResult> CheckMembershipAsync(Guid segmentId, Guid entityId, CancellationToken cancellationToken = default);
}

public class SegmentationService : ISegmentationService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<SegmentationService> _logger;

    public SegmentationService(
        TenantDbContext context,
        ILogger<SegmentationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Guid>> CalculateSegmentMembersAsync(
        Guid segmentId, 
        CancellationToken cancellationToken = default)
    {
        var segment = await _context.Set<Segment>()
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);

        if (segment == null)
            throw new InvalidOperationException($"Segment {segmentId} not found");

        if (segment.Type == SegmentType.Static)
        {
            // For static segments, return manually added members from segment
            // Static segments would need a separate SegmentMember table or use Campaign.Members
            // For now, return empty list - static segments will be managed separately
            return new List<Guid>();
        }

        // Dynamic segment - evaluate filters
        var criteria = JsonSerializer.Deserialize<SegmentCriteria>(segment.FilterCriteria);
        if (criteria == null || !criteria.Filters.Any())
            return new List<Guid>();

        return await EvaluateSegmentFiltersAsync(criteria, segment.EntityType, cancellationToken);
    }

    public async Task<List<Guid>> EvaluateSegmentFiltersAsync(
        SegmentCriteria criteria, 
        string entityType, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            switch (entityType.ToLowerInvariant())
            {
                case "customer":
                    return await EvaluateCustomerFilters(criteria, cancellationToken);
                
                case "lead":
                    return await EvaluateLeadFilters(criteria, cancellationToken);
                
                case "contact":
                    return await EvaluateContactFilters(criteria, cancellationToken);
                
                default:
                    throw new InvalidOperationException($"Unsupported entity type: {entityType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating segment filters for {EntityType}", entityType);
            throw;
        }
    }

    public async Task UpdateSegmentMemberCountAsync(
        Guid segmentId, 
        CancellationToken cancellationToken = default)
    {
        var segment = await _context.Set<Segment>()
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);

        if (segment == null)
            return;

        var members = await CalculateSegmentMembersAsync(segmentId, cancellationToken);
        
        segment.MemberCount = members.Count;
        segment.LastCalculatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated segment {SegmentId} member count to {Count}",
            segmentId, members.Count);
    }

    public async Task<SegmentMembershipResult> CheckMembershipAsync(
        Guid segmentId, 
        Guid entityId, 
        CancellationToken cancellationToken = default)
    {
        var members = await CalculateSegmentMembersAsync(segmentId, cancellationToken);
        var isMember = members.Contains(entityId);

        return new SegmentMembershipResult
        {
            SegmentId = segmentId,
            EntityId = entityId,
            IsMember = isMember,
            CheckedAt = DateTime.UtcNow
        };
    }

    private async Task<List<Guid>> EvaluateCustomerFilters(
        SegmentCriteria criteria, 
        CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsQueryable();

        // Build dynamic query from filters
        var whereClause = BuildWhereClause(criteria);
        if (!string.IsNullOrEmpty(whereClause))
        {
            query = query.Where(whereClause);
        }

        return await query.Select(c => c.Id).ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> EvaluateLeadFilters(
        SegmentCriteria criteria, 
        CancellationToken cancellationToken)
    {
        var query = _context.Leads.AsQueryable();

        var whereClause = BuildWhereClause(criteria);
        if (!string.IsNullOrEmpty(whereClause))
        {
            query = query.Where(whereClause);
        }

        return await query.Select(l => l.Id).ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> EvaluateContactFilters(
        SegmentCriteria criteria, 
        CancellationToken cancellationToken)
    {
        var query = _context.Contacts.AsQueryable();

        var whereClause = BuildWhereClause(criteria);
        if (!string.IsNullOrEmpty(whereClause))
        {
            query = query.Where(whereClause);
        }

        return await query.Select(c => c.Id).ToListAsync(cancellationToken);
    }

    private string BuildWhereClause(SegmentCriteria criteria)
    {
        if (!criteria.Filters.Any())
            return string.Empty;

        var conditions = new List<string>();

        foreach (var filter in criteria.Filters)
        {
            var condition = BuildFilterCondition(filter);
            if (!string.IsNullOrEmpty(condition))
                conditions.Add(condition);
        }

        if (!conditions.Any())
            return string.Empty;

        var logicOp = criteria.LogicOperator.Equals("Or", StringComparison.OrdinalIgnoreCase) 
            ? " || " 
            : " && ";

        return string.Join(logicOp, conditions.Select(c => $"({c})"));
    }

    private string BuildFilterCondition(SegmentFilter filter)
    {
        var field = filter.Field;
        var op = filter.Operator;
        var value = filter.Value;

        // Handle null values
        if (value == null)
        {
            return op.Equals("IsNull", StringComparison.OrdinalIgnoreCase)
                ? $"{field} == null"
                : $"{field} != null";
        }

        // Convert value to proper format
        var valueStr = value is string 
            ? $"\"{value}\"" 
            : value.ToString();

        return op.ToLowerInvariant() switch
        {
            "equals" => $"{field} == {valueStr}",
            "notequals" => $"{field} != {valueStr}",
            "contains" => $"{field}.Contains({valueStr})",
            "startswith" => $"{field}.StartsWith({valueStr})",
            "endswith" => $"{field}.EndsWith({valueStr})",
            "greaterthan" => $"{field} > {valueStr}",
            "lessthan" => $"{field} < {valueStr}",
            "greaterthanorequal" => $"{field} >= {valueStr}",
            "lessthanorequal" => $"{field} <= {valueStr}",
            "in" => BuildInCondition(field, value),
            "between" => BuildBetweenCondition(field, value),
            _ => $"{field} == {valueStr}"
        };
    }

    private string BuildInCondition(string field, object value)
    {
        if (value is not IEnumerable<object> values)
            return string.Empty;

        var valueList = string.Join(", ", values.Select(v => 
            v is string ? $"\"{v}\"" : v.ToString()));
        
        return $"new[] {{ {valueList} }}.Contains({field})";
    }

    private string BuildBetweenCondition(string field, object value)
    {
        if (value is not object[] range || range.Length != 2)
            return string.Empty;

        var min = range[0] is string ? $"\"{range[0]}\"" : range[0].ToString();
        var max = range[1] is string ? $"\"{range[1]}\"" : range[1].ToString();

        return $"{field} >= {min} && {field} <= {max}";
    }
}

public class SegmentMembershipResult
{
    public Guid SegmentId { get; set; }
    public Guid EntityId { get; set; }
    public bool IsMember { get; set; }
    public DateTime CheckedAt { get; set; }
}
