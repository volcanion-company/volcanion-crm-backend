# Contributing to CRM SaaS

Thank you for considering contributing to CRM SaaS! This document provides guidelines and instructions for contributing.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Pull Request Process](#pull-request-process)
- [Issue Guidelines](#issue-guidelines)
- [Testing](#testing)

---

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. By participating in this project, you agree to:

- Be respectful and inclusive
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

### Unacceptable Behavior

- Harassment, discrimination, or offensive comments
- Trolling or insulting/derogatory comments
- Public or private harassment
- Publishing others' private information without permission

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (version 10.0.100+)
- [SQL Server 2022](https://www.microsoft.com/sql-server) or Docker
- [Visual Studio 2022 17.12+](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/crm-saas.git
   cd crm-saas/backend
   ```

3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/crmsaas/crm-saas.git
   ```

---

## Development Setup

### 1. Configure Database

```bash
# Using Docker (recommended)
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password" \
  -p 1433:1433 --name sql_server -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Update Configuration

Edit `src/CrmSaas.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CrmSaas_Master_Dev;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  }
}
```

### 3. Run Migrations

```bash
cd src/CrmSaas.Api
dotnet ef database update --context MasterDbContext
dotnet ef database update --context TenantDbContext
```

### 4. Run Application

```bash
dotnet run
```

---

## Coding Standards

### C# Style Guide

We follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

#### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `CustomerService` |
| Interfaces | IPascalCase | `ICustomerService` |
| Methods | PascalCase | `GetCustomerById` |
| Properties | PascalCase | `FirstName` |
| Private fields | _camelCase | `_customerRepository` |
| Parameters | camelCase | `customerId` |
| Constants | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |

#### Code Organization

```csharp
// 1. Using statements (sorted)
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CrmSaas.Api.Services;

namespace CrmSaas.Api.Controllers;

// 2. Class declaration
public class CustomersController : BaseController
{
    // 3. Private fields
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    // 4. Constructor
    public CustomersController(
        ICustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    // 5. Public methods
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Implementation
    }

    // 6. Private methods
    private void ValidateRequest(CustomerRequest request)
    {
        // Implementation
    }
}
```

### API Guidelines

#### REST Conventions

| Action | HTTP Method | Route | Example |
|--------|-------------|-------|---------|
| List | GET | `/api/{resource}` | `GET /api/customers` |
| Get | GET | `/api/{resource}/{id}` | `GET /api/customers/123` |
| Create | POST | `/api/{resource}` | `POST /api/customers` |
| Update | PUT | `/api/{resource}/{id}` | `PUT /api/customers/123` |
| Delete | DELETE | `/api/{resource}/{id}` | `DELETE /api/customers/123` |

#### Response Format

```json
{
  "success": true,
  "data": { },
  "message": "Operation successful",
  "errors": []
}
```

### Entity Guidelines

- Inherit from `TenantAuditableEntity` for tenant-scoped entities
- Use Data Annotations for validation
- Add navigation properties for relationships
- Use enums for status fields

```csharp
public class Customer : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    
    // Navigation
    public virtual ICollection<Contact> Contacts { get; set; } = [];
}
```

---

## Pull Request Process

### Before Submitting

1. **Update your fork**:
   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   ```

2. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes** following coding standards

4. **Run tests**:
   ```bash
   dotnet test
   ```

5. **Format code**:
   ```bash
   dotnet format
   ```

### Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Formatting, missing semicolons, etc.
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `test`: Adding missing tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(customers): add customer merge functionality
fix(workflows): resolve null reference in action executor
docs(readme): update API documentation
```

### Pull Request Template

When creating a PR, include:

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## How Has This Been Tested?
Describe tests run

## Checklist
- [ ] Code follows project coding standards
- [ ] Self-review completed
- [ ] Code is commented where necessary
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Tests added/updated
- [ ] All tests pass
```

---

## Issue Guidelines

### Bug Reports

Include:
- Clear, descriptive title
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details (.NET version, OS, etc.)
- Screenshots if applicable

### Feature Requests

Include:
- Clear description of the feature
- Use case / motivation
- Proposed solution
- Alternative solutions considered

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task GetCustomerById_WhenExists_ReturnsCustomer()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var expectedCustomer = new Customer { Id = customerId, Name = "Test" };
    _mockRepository.Setup(x => x.GetByIdAsync(customerId))
        .ReturnsAsync(expectedCustomer);

    // Act
    var result = await _service.GetByIdAsync(customerId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedCustomer.Name, result.Name);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/CrmSaas.Api.Tests
```

---

## Questions?

- Open an issue with the `question` label
- Contact maintainers at: dev@crmsaas.com

---

**Thank you for contributing! 🎉**
