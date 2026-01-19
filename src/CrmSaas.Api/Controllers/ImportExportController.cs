using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CrmSaas.Api.Controllers;

[Authorize]
public class ImportExportController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ImportExportController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    #region Customer Export/Import

    [HttpGet("customers/export")]
    [RequirePermission(Permissions.CustomerExport)]
    public async Task<IActionResult> ExportCustomers(
        [FromQuery] string format = "xlsx",
        [FromQuery] CustomerType? type = null,
        [FromQuery] CustomerStatus? status = null)
    {
        var query = _db.Customers
            .AsNoTracking()
            .WhereIf(type.HasValue, c => c.Type == type!.Value)
            .WhereIf(status.HasValue, c => c.Status == status!.Value);

        var customers = await query
            .Select(c => new CustomerExportDto
            {
                Name = c.Name,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                Email = c.Email,
                Phone = c.Phone,
                Website = c.Website,
                Industry = c.Industry,
                CompanyName = c.CompanyName,
                FirstName = c.FirstName,
                LastName = c.LastName,
                AddressLine1 = c.AddressLine1,
                City = c.City,
                State = c.State,
                PostalCode = c.PostalCode,
                Country = c.Country,
                Source = c.Source.ToString(),
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        await _auditService.LogAsync(AuditActions.Export, nameof(Customer), null, $"Exported {customers.Count} customers");

        return format.ToLower() switch
        {
            "csv" => ExportToCsv(customers, "customers"),
            "json" => ExportToJson(customers, "customers"),
            _ => ExportToExcel(customers, "customers")
        };
    }

    [HttpPost("customers/import")]
    [RequirePermission(Permissions.CustomerImport)]
    public async Task<ActionResult<ApiResponse<ImportResult>>> ImportCustomers(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequestResponse<ImportResult>("File is required");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        List<CustomerExportDto> records;
        try
        {
            records = extension switch
            {
                ".csv" => await ParseCsv<CustomerExportDto>(file),
                ".xlsx" or ".xls" => await ParseExcel<CustomerExportDto>(file),
                ".json" => await ParseJson<CustomerExportDto>(file),
                _ => throw new NotSupportedException($"File format {extension} is not supported")
            };
        }
        catch (Exception ex)
        {
            return BadRequestResponse<ImportResult>($"Failed to parse file: {ex.Message}");
        }

        var result = new ImportResult
        {
            TotalRecords = records.Count
        };

        foreach (var record in records)
        {
            try
            {
                var customer = new Customer
                {
                    Name = record.Name,
                    Type = Enum.TryParse<CustomerType>(record.Type, out var t) ? t : CustomerType.Business,
                    Status = Enum.TryParse<CustomerStatus>(record.Status, out var s) ? s : CustomerStatus.Active,
                    Email = record.Email,
                    Phone = record.Phone,
                    Website = record.Website,
                    Industry = record.Industry,
                    CompanyName = record.CompanyName,
                    FirstName = record.FirstName,
                    LastName = record.LastName,
                    AddressLine1 = record.AddressLine1,
                    City = record.City,
                    State = record.State,
                    PostalCode = record.PostalCode,
                    Country = record.Country,
                    Source = Enum.TryParse<CustomerSource>(record.Source, out var src) ? src : CustomerSource.Other,
                    CreatedBy = _currentUser.UserId
                };

                _db.Customers.Add(customer);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Row {result.SuccessCount + result.FailedCount}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        
        await _auditService.LogAsync(AuditActions.Import, nameof(Customer), null, 
            $"Imported {result.SuccessCount} customers, {result.FailedCount} failed");

        return OkResponse(result);
    }

    #endregion

    #region Lead Export/Import

    [HttpGet("leads/export")]
    [RequirePermission(Permissions.LeadExport)]
    public async Task<IActionResult> ExportLeads(
        [FromQuery] string format = "xlsx",
        [FromQuery] LeadStatus? status = null)
    {
        var query = _db.Leads
            .AsNoTracking()
            .WhereIf(status.HasValue, l => l.Status == status!.Value);

        var leads = await query
            .Select(l => new LeadExportDto
            {
                Title = l.Title,
                FirstName = l.FirstName,
                LastName = l.LastName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                JobTitle = l.JobTitle,
                Industry = l.Industry,
                Status = l.Status.ToString(),
                Source = l.Source.ToString(),
                Rating = l.Rating.ToString(),
                Score = l.Score,
                EstimatedValue = l.EstimatedValue,
                City = l.City,
                Country = l.Country,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        await _auditService.LogAsync(AuditActions.Export, nameof(Lead), null, $"Exported {leads.Count} leads");

        return format.ToLower() switch
        {
            "csv" => ExportToCsv(leads, "leads"),
            "json" => ExportToJson(leads, "leads"),
            _ => ExportToExcel(leads, "leads")
        };
    }

    [HttpPost("leads/import")]
    [RequirePermission(Permissions.LeadImport)]
    public async Task<ActionResult<ApiResponse<ImportResult>>> ImportLeads(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequestResponse<ImportResult>("File is required");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        List<LeadExportDto> records;
        try
        {
            records = extension switch
            {
                ".csv" => await ParseCsv<LeadExportDto>(file),
                ".xlsx" or ".xls" => await ParseExcel<LeadExportDto>(file),
                ".json" => await ParseJson<LeadExportDto>(file),
                _ => throw new NotSupportedException($"File format {extension} is not supported")
            };
        }
        catch (Exception ex)
        {
            return BadRequestResponse<ImportResult>($"Failed to parse file: {ex.Message}");
        }

        var result = new ImportResult
        {
            TotalRecords = records.Count
        };

        foreach (var record in records)
        {
            try
            {
                var lead = new Lead
                {
                    Title = record.Title ?? $"Lead: {record.FirstName} {record.LastName}",
                    FirstName = record.FirstName,
                    LastName = record.LastName,
                    Email = record.Email,
                    Phone = record.Phone,
                    CompanyName = record.CompanyName,
                    JobTitle = record.JobTitle,
                    Industry = record.Industry,
                    Source = Enum.TryParse<LeadSource>(record.Source, out var src) ? src : LeadSource.Other,
                    Rating = Enum.TryParse<LeadRating>(record.Rating, out var r) ? r : LeadRating.Cold,
                    Score = record.Score,
                    EstimatedValue = record.EstimatedValue,
                    City = record.City,
                    Country = record.Country,
                    AssignedToUserId = _currentUser.UserId,
                    CreatedBy = _currentUser.UserId
                };

                _db.Leads.Add(lead);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Row {result.SuccessCount + result.FailedCount}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Import, nameof(Lead), null, 
            $"Imported {result.SuccessCount} leads, {result.FailedCount} failed");

        return OkResponse(result);
    }

    #endregion

    #region Contact Export/Import

    [HttpGet("contacts/export")]
    [RequirePermission(Permissions.ContactExport)]
    public async Task<IActionResult> ExportContacts([FromQuery] string format = "xlsx")
    {
        var contacts = await _db.Contacts
            .AsNoTracking()
            .Include(c => c.Customer)
            .Select(c => new ContactExportDto
            {
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Mobile = c.Mobile,
                JobTitle = c.JobTitle,
                Department = c.Department,
                CustomerName = c.Customer != null ? c.Customer.Name : null,
                IsPrimary = c.IsPrimary,
                City = c.City,
                Country = c.Country,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        await _auditService.LogAsync(AuditActions.Export, nameof(Contact), null, $"Exported {contacts.Count} contacts");

        return format.ToLower() switch
        {
            "csv" => ExportToCsv(contacts, "contacts"),
            "json" => ExportToJson(contacts, "contacts"),
            _ => ExportToExcel(contacts, "contacts")
        };
    }

    #endregion

    #region Template Downloads

    [HttpGet("templates/{entity}")]
    public IActionResult DownloadTemplate(string entity)
    {
        var columns = entity.ToLower() switch
        {
            "customers" => new[] { "Name", "Type", "Status", "Email", "Phone", "Website", "Industry", 
                "CompanyName", "FirstName", "LastName", "AddressLine1", "City", "State", "PostalCode", "Country", "Source" },
            "leads" => new[] { "Title", "FirstName", "LastName", "Email", "Phone", "CompanyName", 
                "JobTitle", "Industry", "Source", "Rating", "Score", "EstimatedValue", "City", "Country" },
            "contacts" => new[] { "FirstName", "LastName", "Email", "Phone", "Mobile", "JobTitle", 
                "Department", "CustomerName", "IsPrimary", "City", "Country" },
            _ => throw new NotSupportedException($"Template for {entity} is not available")
        };

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(entity);
        
        for (int i = 0; i < columns.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = columns[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{entity}_template.xlsx"
        );
    }

    #endregion

    #region Helper Methods

    private FileResult ExportToExcel<T>(List<T> data, string fileName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        var properties = typeof(T).GetProperties();
        
        // Headers
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(data[row]);
                worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{fileName}_{DateTime.UtcNow:yyyyMMdd}.xlsx"
        );
    }

    private FileResult ExportToCsv<T>(List<T> data, string fileName)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        
        csv.WriteRecords(data);
        writer.Flush();

        return File(
            stream.ToArray(),
            "text/csv",
            $"{fileName}_{DateTime.UtcNow:yyyyMMdd}.csv"
        );
    }

    private FileResult ExportToJson<T>(List<T> data, string fileName)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return File(
            Encoding.UTF8.GetBytes(json),
            "application/json",
            $"{fileName}_{DateTime.UtcNow:yyyyMMdd}.json"
        );
    }

    private static async Task<List<T>> ParseCsv<T>(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });
        
        return await Task.Run(() => csv.GetRecords<T>().ToList());
    }

    private static async Task<List<T>> ParseExcel<T>(IFormFile file) where T : new()
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        
        var records = new List<T>();
        var properties = typeof(T).GetProperties();
        var headerRow = worksheet.Row(1);
        
        var columnMap = new Dictionary<int, string>();
        for (int col = 1; col <= worksheet.LastColumnUsed()?.ColumnNumber(); col++)
        {
            columnMap[col] = headerRow.Cell(col).GetString().Trim();
        }

        for (int row = 2; row <= worksheet.LastRowUsed()?.RowNumber(); row++)
        {
            var record = new T();
            foreach (var (col, header) in columnMap)
            {
                var property = properties.FirstOrDefault(p => 
                    p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));
                    
                if (property != null)
                {
                    var cellValue = worksheet.Cell(row, col).GetString();
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(cellValue, 
                                Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                            property.SetValue(record, convertedValue);
                        }
                        catch
                        {
                            // Skip if conversion fails
                        }
                    }
                }
            }
            records.Add(record);
        }

        return await Task.FromResult(records);
    }

    private static async Task<List<T>> ParseJson<T>(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var records = await JsonSerializer.DeserializeAsync<List<T>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return records ?? [];
    }

    #endregion
}

#region Export DTOs

public class CustomerExportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? CompanyName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeadExportDto
{
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Industry { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
    public string? Rating { get; set; }
    public int Score { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ContactExportDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? CustomerName { get; set; }
    public bool IsPrimary { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ImportResult
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = [];
}

#endregion
