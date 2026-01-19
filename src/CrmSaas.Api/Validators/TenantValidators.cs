using FluentValidation;
using CrmSaas.Api.Services;

namespace CrmSaas.Api.Validators;

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Identifier is required")
            .MaximumLength(50).WithMessage("Identifier must not exceed 50 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Identifier can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Subdomain)
            .MaximumLength(50).WithMessage("Subdomain must not exceed 50 characters")
            .Matches("^[a-z0-9-]*$").WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.Subdomain));
    }
}

public class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage("MaxUsers must be greater than 0")
            .When(x => x.MaxUsers.HasValue);
    }
}
