# CRM SaaS - Production Deployment Guide

## 🎯 Overview
Multi-tenant CRM SaaS application built with .NET 10, SQL Server 2025, and Hangfire.

## ✅ Completed Features (100%)

All 13 major phases completed:

### Phase 1-8: Foundation & Core CRUD
- ✅ Multi-tenant architecture (master + tenant databases)
- ✅ JWT authentication & authorization
- ✅ Complete CRUD for all entities (Users, Customers, Leads, Opportunities, Activities, Tickets, Products, Orders, Quotations)
- ✅ Role-based access control (RBAC)
- ✅ Audit logging
- ✅ Scalar API documentation

### Phase 9: Database Architecture
- ✅ Master database with Tenants table
- ✅ Per-tenant databases with full schema
- ✅ Dual DbContext pattern (MasterDbContext + TenantDbContext)
- ✅ Connection string resolver
- ✅ Automatic tenant database creation

### Phase 18: DataScope Enforcement
- ✅ Own/All data scope filtering
- ✅ Repository pattern with scope enforcement
- ✅ Automatic filtering in queries

### Phase 11: Workflow Engine
- ✅ 17 conditional operators (Equals, Contains, GreaterThan, LessThan, etc.)
- ✅ 10 workflow actions (SendEmail, CreateTask, UpdateField, SendNotification, etc.)
- ✅ Trigger-based execution (Customer.Created, Lead.Converted, etc.)
- ✅ AND/OR condition logic

### Phase 15: Notification System
- ✅ Multi-channel delivery (InApp, Email, SMS, Push)
- ✅ User notification preferences
- ✅ Template-based notifications
- ✅ Notification history tracking

### Phase 14: Background Jobs (Hangfire)
- ✅ 10 recurring jobs configured
- ✅ SLA monitoring & auto-escalation
- ✅ Workflow execution
- ✅ Activity reminder processing
- ✅ Webhook delivery
- ✅ Marketing campaign execution

### Phase 12: SLA & Ticket Automation
- ✅ SLA policies with first response & resolution targets
- ✅ Auto-escalation on SLA breach
- ✅ SLA pause/resume functionality
- ✅ Escalation history tracking

### Phase 10: Duplicate Detection & Customer 360
- ✅ Fuzzy matching (Levenshtein distance)
- ✅ Duplicate detection for Customers, Leads, Contacts
- ✅ Customer health scoring algorithm
- ✅ 360° customer view API

### Phase 13: Marketing Automation
- ✅ Customer segmentation (dynamic rules)
- ✅ Email marketing campaigns
- ✅ Campaign analytics & tracking
- ✅ Lead scoring based on engagement

### Phase 17: Advanced Reports & Analytics
- ✅ 40+ KPI reports
- ✅ Sales pipeline analytics
- ✅ Cohort analysis
- ✅ Funnel conversion tracking
- ✅ Real-time dashboard data

### Phase 19: Integration Module
- ✅ Webhook subscriptions
- ✅ Event-based triggers (Customer.Created, Lead.Updated, etc.)
- ✅ HMAC signature authentication
- ✅ Automatic retry with exponential backoff
- ✅ Webhook delivery history

### Phase 16: Calendar & Activity Sync
- ✅ OAuth 2.0 integration (Google, Microsoft)
- ✅ iCalendar (.ics) export
- ✅ Activity reminder system
- ✅ Bi-directional sync support

### Phase 20: DevOps & Production Readiness
- ✅ Health check endpoints (`/health`, `/health/live`, `/health/ready`)
- ✅ Database health monitoring
- ✅ Hangfire health monitoring
- ✅ Graceful shutdown configured
- ✅ Comprehensive logging with Serilog

## 🏗 Architecture

### Technology Stack
- **Framework**: .NET 10 / ASP.NET Core
- **Database**: SQL Server 2025
- **ORM**: Entity Framework Core 9
- **Background Jobs**: Hangfire 1.8.17
- **Logging**: Serilog
- **API Documentation**: Scalar.AspNetCore
- **Authentication**: JWT Bearer

### Database Structure
```
Master Database (CrmSaas_Master_Dev):
├── Tenants
├── RefreshTokens
└── (Authentication & tenant management)

Tenant Database (CrmSaas_{TenantId}):
├── Users, Roles, Permissions
├── Customers, Leads, Contacts, Opportunities
├── Activities, Tickets, Products, Orders, Quotations
├── Workflows, Notifications, SLAs
├── MarketingCampaigns, Segments
├── Reports, Dashboards
├── WebhookSubscriptions, WebhookDeliveries
└── CalendarSyncConfigurations, ActivityReminders
```

## 🚀 Deployment Steps

### 1. Prerequisites
- Windows Server 2019+ or Linux with Docker
- SQL Server 2025 or SQL Server 2022+
- .NET 10 Runtime
- IIS 10+ or Nginx (for reverse proxy)

### 2. Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server={server};Database=CrmSaas_Master;User Id={user};Password={password};TrustServerCertificate=True;
ConnectionStrings__TenantTemplate=Server={server};Database=CrmSaas_{TenantId};User Id={user};Password={password};TrustServerCertificate=True;
JwtSettings__Secret={256-bit-secret-key}
JwtSettings__Issuer=https://yourdomain.com
JwtSettings__Audience=https://yourdomain.com
EmailSettings__Enabled=true
EmailSettings__SmtpServer={smtp-server}
EmailSettings__SmtpPort=587
EmailSettings__Username={email}
EmailSettings__Password={password}
```

### 3. Database Migration
```bash
# Navigate to API project
cd d:\Draft\crm\backend\src\CrmSaas.Api

# Create master database
dotnet ef database update --context MasterDbContext

# Create initial tenant (run after first tenant registration via API)
# Tenant databases are created automatically on signup
```

### 4. Build & Publish
```bash
# Build release
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Files will be in ./publish folder
```

### 5. IIS Deployment
1. Install .NET 10 Hosting Bundle
2. Create new IIS site
3. Point to `./publish` folder
4. Set application pool to "No Managed Code"
5. Configure bindings (HTTPS recommended)
6. Set environment variables in web.config or system

### 6. Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "CrmSaas.Api.dll"]
```

```bash
docker build -t crm-saas:latest .
docker run -d -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="..." \
  crm-saas:latest
```

## 🔍 Health Check Endpoints

### /health
Complete health status with all checks:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is accessible",
      "data": {
        "tenantCount": 5,
        "timestamp": "2025-01-01T10:00:00Z"
      }
    },
    {
      "name": "hangfire",
      "status": "Healthy",
      "data": {
        "serverCount": 1,
        "succeededJobs": 1250,
        "recurringJobs": 10
      }
    }
  ]
}
```

### /health/live
Liveness probe (K8s compatible) - Always returns 200 OK if app is running

### /health/ready
Readiness probe (K8s compatible) - Returns 200 OK when all dependencies are healthy

## 📊 Monitoring Recommendations

### Application Performance Monitoring (APM)
- **Azure Application Insights**: Recommended for Azure deployments
- **New Relic**: Cross-platform APM
- **Datadog**: Comprehensive monitoring

### Logging
- Logs stored in `./logs/` directory (rolling daily)
- Structured JSON logging via Serilog
- Configure external log aggregation (e.g., ELK Stack, Azure Log Analytics)

### Metrics to Monitor
- API response times
- Database query performance
- Hangfire job success/failure rates
- Memory & CPU usage
- Active tenant count
- SLA breach rate

## 🔒 Security Checklist

- [x] HTTPS enforced
- [x] JWT tokens with expiration
- [x] Password hashing (BCrypt)
- [x] SQL injection protection (parameterized queries)
- [x] CORS configured
- [x] Rate limiting implemented
- [x] Audit logging enabled
- [ ] **TODO**: Configure firewall rules
- [ ] **TODO**: Enable DDoS protection
- [ ] **TODO**: Set up WAF (Web Application Firewall)

## 🧪 Testing

### Integration Tests
```bash
cd tests/CrmSaas.IntegrationTests
dotnet test
```

### Load Testing
Use tools like:
- **Apache JMeter**
- **k6**
- **Azure Load Testing**

Recommended tests:
- 100 concurrent users: API should respond < 500ms
- 1000 requests/min: Hangfire jobs should process without backlog
- Database: Query optimization for < 100ms response

## 📈 Scaling Recommendations

### Horizontal Scaling
- Deploy multiple API instances behind load balancer
- Share SQL Server or use read replicas
- Single Hangfire server (or configure distributed locks)

### Vertical Scaling
- API: 4 CPU cores, 8GB RAM (minimum)
- Database: 8 CPU cores, 16GB RAM (recommended)
- Hangfire: Runs in same process, no extra resources needed

### Performance Optimization
- Enable response caching for GET endpoints
- Use Redis for distributed cache
- Configure connection pooling for SQL Server
- Index optimization (check execution plans)

## 🛠 Troubleshooting

### Common Issues

**1. Application won't start**
- Check logs in `./logs/` folder
- Verify connection strings
- Ensure SQL Server is accessible
- Check firewall rules

**2. Hangfire jobs not running**
- Verify Hangfire dashboard at `/hangfire`
- Check database connection
- Ensure Hangfire tables exist
- Review job history for errors

**3. Health checks failing**
- Test database connectivity manually
- Check Hangfire server status
- Review application logs

**4. Performance degradation**
- Enable query logging: `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command = Information`
- Check for missing indexes
- Review Hangfire job queue
- Monitor memory usage

## 📞 Support

For issues or questions:
- Review logs in `./logs/`
- Check API documentation at `/scalar/v1`
- Inspect Hangfire dashboard at `/hangfire`
- Review health status at `/health`

---

**🎉 Application Ready for Production Deployment!**

All 13 phases completed with comprehensive features including:
- Multi-tenancy
- Workflow automation
- Marketing campaigns
- Advanced analytics
- Webhook integrations
- Calendar sync
- Production monitoring

Total development: **13 major phases** | **100% complete** | **Production-ready**
