namespace CrmSaas.Api.Authorization;

public static class Permissions
{
    // Customer
    public const string CustomerView = "customer.view";
    public const string CustomerCreate = "customer.create";
    public const string CustomerUpdate = "customer.update";
    public const string CustomerDelete = "customer.delete";
    public const string CustomerExport = "customer.export";
    public const string CustomerImport = "customer.import";

    // Contact
    public const string ContactView = "contact.view";
    public const string ContactCreate = "contact.create";
    public const string ContactUpdate = "contact.update";
    public const string ContactDelete = "contact.delete";
    public const string ContactExport = "contact.export";
    public const string ContactImport = "contact.import";

    // Lead
    public const string LeadView = "lead.view";
    public const string LeadCreate = "lead.create";
    public const string LeadUpdate = "lead.update";
    public const string LeadDelete = "lead.delete";
    public const string LeadAssign = "lead.assign";
    public const string LeadConvert = "lead.convert";
    public const string LeadExport = "lead.export";
    public const string LeadImport = "lead.import";

    // Opportunity
    public const string OpportunityView = "opportunity.view";
    public const string OpportunityCreate = "opportunity.create";
    public const string OpportunityUpdate = "opportunity.update";
    public const string OpportunityDelete = "opportunity.delete";
    public const string OpportunityAssign = "opportunity.assign";
    public const string OpportunityExport = "opportunity.export";
    public const string OpportunityImport = "opportunity.import";

    // Pipeline
    public const string PipelineView = "pipeline.view";
    public const string PipelineCreate = "pipeline.create";
    public const string PipelineUpdate = "pipeline.update";
    public const string PipelineDelete = "pipeline.delete";

    // Quotation
    public const string QuotationView = "quotation.view";
    public const string QuotationCreate = "quotation.create";
    public const string QuotationUpdate = "quotation.update";
    public const string QuotationDelete = "quotation.delete";

    // Order
    public const string OrderView = "order.view";
    public const string OrderCreate = "order.create";
    public const string OrderUpdate = "order.update";
    public const string OrderDelete = "order.delete";
    public const string OrderExport = "order.export";
    public const string OrderImport = "order.import";

    // Contract
    public const string ContractView = "contract.view";
    public const string ContractCreate = "contract.create";
    public const string ContractUpdate = "contract.update";
    public const string ContractDelete = "contract.delete";
    public const string ContractExport = "contract.export";
    public const string ContractImport = "contract.import";

    // Ticket
    public const string TicketView = "ticket.view";
    public const string TicketCreate = "ticket.create";
    public const string TicketUpdate = "ticket.update";
    public const string TicketDelete = "ticket.delete";
    public const string TicketAssign = "ticket.assign";
    public const string TicketResolve = "ticket.resolve";
    public const string TicketEscalate = "ticket.escalate";
    public const string TicketExport = "ticket.export";
    public const string TicketImport = "ticket.import";

    // SLA
    public const string SlaView = "sla.view";
    public const string SlaCreate = "sla.create";
    public const string SlaUpdate = "sla.update";
    public const string SlaDelete = "sla.delete";

    // Campaign
    public const string CampaignView = "campaign.view";
    public const string CampaignCreate = "campaign.create";
    public const string CampaignUpdate = "campaign.update";
    public const string CampaignDelete = "campaign.delete";
    public const string CampaignExport = "campaign.export";
    public const string CampaignImport = "campaign.import";

    // Activity
    public const string ActivityView = "activity.view";
    public const string ActivityCreate = "activity.create";
    public const string ActivityUpdate = "activity.update";
    public const string ActivityDelete = "activity.delete";
    public const string ActivityExport = "activity.export";
    public const string ActivityImport = "activity.import";

    // Report
    public const string ReportView = "report.view";
    public const string ReportCreate = "report.create";
    public const string ReportUpdate = "report.update";
    public const string ReportDelete = "report.delete";
    public const string ReportExport = "report.export";
    public const string ReportImport = "report.import";

    // User
    public const string UserView = "user.view";
    public const string UserCreate = "user.create";
    public const string UserUpdate = "user.update";
    public const string UserDelete = "user.delete";
    public const string UserExport = "user.export";
    public const string UserImport = "user.import";

    // Role
    public const string RoleView = "role.view";
    public const string RoleCreate = "role.create";
    public const string RoleUpdate = "role.update";
    public const string RoleDelete = "role.delete";
    public const string RoleExport = "role.export";
    public const string RoleImport = "role.import";

    // Tenant
    public const string TenantView = "tenant.view";
    public const string TenantCreate = "tenant.create";
    public const string TenantUpdate = "tenant.update";
    public const string TenantDelete = "tenant.delete";
    public const string TenantSettings = "tenant.settings";
    public const string TenantExport = "tenant.export";
    public const string TenantImport = "tenant.import";

    // Audit
    public const string AuditView = "audit.view";

    // Workflow
    public const string WorkflowView = "workflow.view";
    public const string WorkflowCreate = "workflow.create";
    public const string WorkflowEdit = "workflow.edit";
    public const string WorkflowDelete = "workflow.delete";
    public const string WorkflowExecute = "workflow.execute";
}
