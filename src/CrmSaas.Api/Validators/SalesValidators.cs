using FluentValidation;
using CrmSaas.Api.Controllers;

namespace CrmSaas.Api.Validators;

public class CreateLeadRequestValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.EstimatedValue)
            .GreaterThanOrEqualTo(0).WithMessage("EstimatedValue must be non-negative")
            .When(x => x.EstimatedValue.HasValue);
    }
}

public class CreateOpportunityRequestValidator : AbstractValidator<CreateOpportunityRequest>
{
    public CreateOpportunityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.PipelineId)
            .NotEmpty().WithMessage("PipelineId is required");

        RuleFor(x => x.StageId)
            .NotEmpty().WithMessage("StageId is required");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be non-negative");

        RuleFor(x => x.Probability)
            .InclusiveBetween(0, 100).WithMessage("Probability must be between 0 and 100");
    }
}
