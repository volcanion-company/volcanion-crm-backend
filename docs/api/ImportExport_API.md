# Import/Export API Documentation

## Tổng quan Module

### Import/Export là gì?

**Import/Export** là tính năng cho phép users migrate data vào/ra khỏi CRM thông qua file (Excel, CSV, JSON).

### Tại sao cần Import/Export?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    WHY IMPORT/EXPORT IS ESSENTIAL                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Import Use Cases:                                                          │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Initial data migration (from old CRM/spreadsheets)                       │
│  • Bulk data entry (500 leads from marketing campaign)                      │
│  • Regular updates (monthly customer list from ERP)                         │
│  • Partner data integration (leads from affiliate)                          │
│                                                                             │
│  Without Import:                                                            │
│  • Manual entry of 500 leads → 20+ hours of work                            │
│  • Error-prone, inconsistent formatting                                     │
│  • Delayed go-live for new customers                                        │
│                                                                             │
│  With Import:                                                               │
│  • Upload CSV → 500 leads in 30 seconds                                     │
│  • Validation, error reporting, rollback on failure                         │
│  • Instant productivity                                                     │
│                                                                             │
│  Export Use Cases:                                                          │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Backup data (monthly export to secure storage)                           │
│  • Reporting in Excel/Power BI (CFO wants pivot tables)                     │
│  • Integration with other systems (sync to marketing automation)            │
│  • Data analysis offline (sales team wants to analyze in Excel)             │
│  • Compliance (customer requests their data - GDPR)                         │
│                                                                             │
│  Without Export:                                                            │
│  • Manual copy-paste → hours of work                                        │
│  • Data stuck in system, can't analyze elsewhere                            │
│  • Can't fulfill GDPR data portability requests                             │
│                                                                             │
│  With Export:                                                               │
│  • Click "Export" → Download 10,000 customers in Excel                      │
│  • JSON export for API integrations                                         │
│  • CSV for bulk analysis                                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Import/Export Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       IMPORT/EXPORT FLOW                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  EXPORT FLOW:                                                               │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  1. User Request                                                            │
│     GET /api/import-export/customers/export?format=xlsx                     │
│              ↓                                                              │
│  2. Query Database                                                          │
│     SELECT * FROM Customers WHERE TenantId = 'tenant-guid'                  │
│              ↓                                                              │
│  3. Transform to DTO                                                        │
│     Customer entities → CustomerExportDto (flat structure)                  │
│              ↓                                                              │
│  4. Generate File                                                           │
│     ┌────────────────────────────────────────┐                             │
│     │ Format: XLSX                           │                             │
│     │ • Create workbook                      │                             │
│     │ • Add headers (Name, Email, Phone...)  │                             │
│     │ • Add data rows                        │                             │
│     │ • Apply formatting (bold headers)      │                             │
│     │ • Auto-fit columns                     │                             │
│     └────────────────────────────────────────┘                             │
│              ↓                                                              │
│  5. Return File                                                             │
│     Content-Type: application/vnd.openxmlformats...                         │
│     Content-Disposition: attachment; filename="customers_20260118.xlsx"     │
│              ↓                                                              │
│  6. User Downloads                                                          │
│     Browser saves file to Downloads folder                                  │
│                                                                             │
│  ───────────────────────────────────────────────────────────────────────    │
│                                                                             │
│  IMPORT FLOW:                                                               │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  1. User Upload                                                             │
│     POST /api/import-export/leads/import                                    │
│     File: leads.xlsx (500 rows)                                             │
│              ↓                                                              │
│  2. Parse File                                                              │
│     ┌────────────────────────────────────────┐                             │
│     │ Read Excel workbook                    │                             │
│     │ • Map headers to DTO properties        │                             │
│     │ • Read each row                        │                             │
│     │ • Convert cell values to types         │                             │
│     │ • Handle missing/invalid data          │                             │
│     └────────────────────────────────────────┘                             │
│              ↓                                                              │
│  3. Validate & Transform                                                    │
│     For each record:                                                        │
│       • Required field validation                                           │
│       • Enum parsing (Status: "Active" → enum)                              │
│       • Data type conversion                                                │
│       • Business logic validation                                           │
│              ↓                                                              │
│  4. Insert to Database                                                      │
│     Transaction:                                                            │
│       foreach (record in records)                                           │
│       {                                                                     │
│         try {                                                               │
│           db.Leads.Add(record);                                             │
│           successCount++;                                                   │
│         } catch {                                                           │
│           failedCount++;                                                    │
│           errors.Add("Row 123: Invalid email");                             │
│         }                                                                   │
│       }                                                                     │
│       db.SaveChanges();                                                     │
│              ↓                                                              │
│  5. Return Result                                                           │
│     {                                                                       │
│       "totalRecords": 500,                                                  │
│       "successCount": 485,                                                  │
│       "failedCount": 15,                                                    │
│       "errors": [                                                           │
│         "Row 23: Email format invalid",                                     │
│         "Row 45: Required field 'FirstName' missing",                       │
│         ...                                                                 │
│       ]                                                                     │
│     }                                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Supported Entities

| Entity | Import | Export | Description |
|--------|:------:|:------:|-------------|
| **Customers** | ✅ | ✅ | Customer records with full details |
| **Leads** | ✅ | ✅ | Lead information for sales pipeline |
| **Contacts** | ✅ | ✅ | Contact persons linked to customers |
| **Opportunities** | ❌ | ✅ | Deals in pipeline (export only) |
| **Tickets** | ❌ | ✅ | Support tickets (export only) |
| **Activities** | ❌ | ✅ | Tasks, calls, meetings (export only) |

---

## Supported File Formats

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        FILE FORMAT COMPARISON                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Format: XLSX (Excel)                                                       │
│  ─────────────────────────────────────────────────────────────────────     │
│  ✅ Pros:                                                                    │
│    • Human-friendly (open in Excel)                                         │
│    • Formatted headers (bold, colors)                                       │
│    • Multiple sheets possible                                               │
│    • Column auto-sizing                                                     │
│  ❌ Cons:                                                                    │
│    • Larger file size                                                       │
│    • Slower parsing                                                         │
│  📊 Best for: Manual data entry, business users                             │
│                                                                             │
│  Format: CSV                                                                │
│  ─────────────────────────────────────────────────────────────────────     │
│  ✅ Pros:                                                                    │
│    • Lightweight, fast parsing                                              │
│    • Universal format (any system can read)                                 │
│    • Easy to generate programmatically                                      │
│  ❌ Cons:                                                                    │
│    • No formatting                                                          │
│    • Single sheet only                                                      │
│    • Encoding issues (UTF-8 vs Latin-1)                                     │
│  📊 Best for: System integrations, bulk operations                          │
│                                                                             │
│  Format: JSON                                                               │
│  ─────────────────────────────────────────────────────────────────────     │
│  ✅ Pros:                                                                    │
│    • Structured data (nested objects)                                       │
│    • API-friendly                                                           │
│    • Type preservation                                                      │
│  ❌ Cons:                                                                    │
│    • Not human-friendly for large datasets                                  │
│    • Can't open in Excel                                                    │
│  📊 Best for: API integrations, system-to-system transfers                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Export Customers (Xuất customers)

**Nghiệp vụ thực tế:**
- Backup customer data monthly
- Analyze customers in Excel/Power BI

**Ví dụ thực tế:**
> CFO needs customer analysis:
> - Go to Customers page
> - Click "Export" button
> - Select format: XLSX
> - Filter: Type = "Enterprise", Status = "Active"
> - Download: customers_20260118.xlsx (234 records)
> - Open in Excel, create pivot table
> - Analyze: Revenue by industry, geographic distribution
> → Business insights from exported data

---

### 2. Import Customers (Nhập customers)

**Nghiệp vụ thực tế:**
- Migrate from old CRM
- Bulk update from ERP system

**Ví dụ thực tế:**
> Company switching from Salesforce:
> - Export 1,000 customers from Salesforce (CSV)
> - Transform CSV to match template:
>   * Salesforce "Account Name" → "Name"
>   * Salesforce "Account Type" → "Type"
> - Upload to CRM: POST /import-export/customers/import
> - Result:
>   * Total: 1,000
>   * Success: 985
>   * Failed: 15 (invalid emails, missing required fields)
>   * Errors: "Row 23: Email format invalid"
> - Fix 15 errors, re-import
> → 1,000 customers migrated in minutes

---

### 3. Export Leads (Xuất leads)

**Nghiệp vụ thực tế:**
- Share leads with partner
- Marketing campaign analysis

**Ví dụ thực tế:**
> Marketing manager analyzing campaign ROI:
> - Export leads from last quarter
> - Filter: Source = "Google Ads", Status = "Qualified/Converted"
> - Format: CSV
> - Import into Google Sheets
> - Merge with campaign cost data
> - Calculate: Cost per lead, conversion rate
> → $50/lead, 15% conversion → profitable campaign

---

### 4. Import Leads (Nhập leads)

**Nghiệp vụ thực tế:**
- Import leads from trade show
- Bulk upload from marketing list

**Ví dụ thực tế:**
> Sales team attended trade show, collected 500 business cards:
> - Intern enters data into Excel template:
>   * FirstName, LastName, Email, Phone, CompanyName, JobTitle
> - Save as: tradeshow_leads.xlsx
> - Upload via Import page
> - System processes:
>   * 500 rows parsed
>   * 475 success, 25 failed
>   * Errors: "Row 12: Email already exists", "Row 45: Phone format invalid"
> - Assign leads to sales reps
> → 475 new leads ready for follow-up

---

### 5. Download Template (Tải template)

**Nghiệp vụ thực tế:**
- Get correct column format for import
- Avoid import errors

**Ví dụ thực tế:**
> New user wants to import customers:
> - GET /import-export/templates/customers
> - Downloads: customers_template.xlsx
> - Opens file, sees headers:
>   | Name | Type | Status | Email | Phone | Website | Industry | ...
>   |------|------|--------|-------|-------|---------|----------|
>   | (empty rows for data entry)
> - Fill in customer data matching exact column names
> - Upload for import
> → Zero format errors, smooth import

---

## Best Practices

### 1. Template Download First

```
✅ ALWAYS:
1. Download template file
2. Fill data in template
3. Upload filled template

❌ NEVER:
- Create own Excel file with random column names
- Mix column order (Name, Email, Phone vs Phone, Name, Email)
- Use different naming (e.g., "E-mail" instead of "Email")
```

### 2. Data Validation Before Import

```csharp
// Client-side validation (before upload)
public class ImportValidator
{
    public List<string> ValidateLeads(List<LeadImportDto> leads)
    {
        var errors = new List<string>();
        
        for (int i = 0; i < leads.Count; i++)
        {
            var lead = leads[i];
            var row = i + 2; // +2 for header row and 0-based index
            
            // Required fields
            if (string.IsNullOrWhiteSpace(lead.FirstName) || 
                string.IsNullOrWhiteSpace(lead.LastName))
            {
                errors.Add($"Row {row}: FirstName and LastName are required");
            }
            
            // Email format
            if (!string.IsNullOrWhiteSpace(lead.Email) && 
                !IsValidEmail(lead.Email))
            {
                errors.Add($"Row {row}: Invalid email format '{lead.Email}'");
            }
            
            // Phone format
            if (!string.IsNullOrWhiteSpace(lead.Phone) && 
                !IsValidPhone(lead.Phone))
            {
                errors.Add($"Row {row}: Invalid phone format '{lead.Phone}'");
            }
            
            // Enum validation
            if (!string.IsNullOrWhiteSpace(lead.Status) && 
                !Enum.TryParse<LeadStatus>(lead.Status, true, out _))
            {
                errors.Add($"Row {row}: Invalid status '{lead.Status}'. Valid values: New, Contacted, Qualified, Lost");
            }
        }
        
        return errors;
    }
}
```

### 3. Handle Encoding Issues

```csharp
// CSV encoding detection
public static Encoding DetectEncoding(Stream stream)
{
    using var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true);
    reader.Peek(); // Trigger encoding detection
    return reader.CurrentEncoding;
}

// UTF-8 with BOM for Excel compatibility
public FileResult ExportToCsv<T>(List<T> data, string fileName)
{
    var utf8WithBom = new UTF8Encoding(true); // BOM ensures Excel opens correctly
    using var stream = new MemoryStream();
    using var writer = new StreamWriter(stream, utf8WithBom);
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    
    csv.WriteRecords(data);
    writer.Flush();
    
    return File(stream.ToArray(), "text/csv", $"{fileName}.csv");
}
```

### 4. Transaction & Rollback

```csharp
public async Task<ImportResult> ImportCustomersAsync(List<CustomerImportDto> customers)
{
    using var transaction = await _db.Database.BeginTransactionAsync();
    
    var result = new ImportResult { TotalRecords = customers.Count };
    
    try
    {
        foreach (var dto in customers)
        {
            try
            {
                var customer = MapToEntity(dto);
                _db.Customers.Add(customer);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Row {result.SuccessCount + result.FailedCount}: {ex.Message}");
            }
        }
        
        // Only commit if success rate is acceptable
        if (result.FailedCount > 0 && result.FailedCount >= result.TotalRecords * 0.5) // 50% failure rate
        {
            await transaction.RollbackAsync();
            throw new Exception("Import failed: More than 50% of records had errors. No data was imported.");
        }
        
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
    
    return result;
}
```

---

## Technical Overview

**Base URL:** `/api/v1/import-export`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Export Customers

Xuất customers ra file.

```
GET /api/v1/import-export/customers/export
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `format` | string | File format: "xlsx" (default), "csv", "json" |
| `type` | CustomerType | Filter by type (optional) |
| `status` | CustomerStatus | Filter by status (optional) |

**Permission Required:** `customer.export`

**Response:** File download

**File Names:**
- `customers_20260118.xlsx`
- `customers_20260118.csv`
- `customers_20260118.json`

---

### 2. Import Customers

Nhập customers từ file.

```
POST /api/v1/import-export/customers/import
```

**Request:** Multipart form-data with file

**Supported Formats:** .xlsx, .xls, .csv, .json

**Permission Required:** `customer.import`

**Response:**

```json
{
  "success": true,
  "data": {
    "totalRecords": 500,
    "successCount": 485,
    "failedCount": 15,
    "errors": [
      "Row 23: Invalid email format 'notanemail'",
      "Row 45: Required field 'Name' is missing",
      "Row 67: Phone number format invalid '+1234'"
    ]
  }
}
```

---

### 3. Export Leads

Xuất leads ra file.

```
GET /api/v1/import-export/leads/export
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `format` | string | File format: "xlsx" (default), "csv", "json" |
| `status` | LeadStatus | Filter by status (optional) |

**Permission Required:** `lead.export`

**Response:** File download (leads_20260118.xlsx)

---

### 4. Import Leads

Nhập leads từ file.

```
POST /api/v1/import-export/leads/import
```

**Request:** Multipart form-data with file

**Supported Formats:** .xlsx, .xls, .csv, .json

**Permission Required:** `lead.import`

**Response:** Same as Import Customers

---

### 5. Export Contacts

Xuất contacts ra file.

```
GET /api/v1/import-export/contacts/export
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `format` | string | File format: "xlsx" (default), "csv", "json" |

**Permission Required:** `contact.export`

**Response:** File download (contacts_20260118.xlsx)

---

### 6. Download Template

Tải template file để import.

```
GET /api/v1/import-export/templates/{entity}
```

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `entity` | string | Entity type: "customers", "leads", "contacts" |

**Response:** Excel template file with headers (empty rows for data entry)

**Example:**
- GET `/import-export/templates/customers` → customers_template.xlsx
- GET `/import-export/templates/leads` → leads_template.xlsx

---

## Export DTOs

### CustomerExportDto

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Customer name |
| `Type` | string | Business/Individual |
| `Status` | string | Active/Inactive/Suspended |
| `Email` | string | Email address |
| `Phone` | string | Phone number |
| `Website` | string | Website URL |
| `Industry` | string | Industry sector |
| `CompanyName` | string | Company name (for Individual type) |
| `FirstName` | string | First name (for Individual type) |
| `LastName` | string | Last name (for Individual type) |
| `AddressLine1` | string | Street address |
| `City` | string | City |
| `State` | string | State/Province |
| `PostalCode` | string | ZIP/Postal code |
| `Country` | string | Country |
| `Source` | string | Customer source |
| `CreatedAt` | DateTime | Creation date |

### LeadExportDto

| Field | Type | Description |
|-------|------|-------------|
| `Title` | string | Lead title |
| `FirstName` | string | First name |
| `LastName` | string | Last name |
| `Email` | string | Email address |
| `Phone` | string | Phone number |
| `CompanyName` | string | Company name |
| `JobTitle` | string | Job title |
| `Industry` | string | Industry |
| `Status` | string | Lead status |
| `Source` | string | Lead source |
| `Rating` | string | Hot/Warm/Cold |
| `Score` | int | Lead score (0-100) |
| `EstimatedValue` | decimal | Estimated deal value |
| `City` | string | City |
| `Country` | string | Country |
| `CreatedAt` | DateTime | Creation date |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `customer.export` | Export customer data |
| `customer.import` | Import customer data |
| `lead.export` | Export lead data |
| `lead.import` | Import lead data |
| `contact.export` | Export contact data |
| `contact.import` | Import contact data |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
