using FluentValidation;
using Inventory.Api.Tenants;

namespace Inventory.Api.Validation
{
    public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
    {
        public CreateTenantRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(2).MaximumLength(200);

            RuleFor(x => x.Domain)
                .MaximumLength(200)
                .Matches(@"^[a-z0-9\-\.]+$").When(x => !string.IsNullOrWhiteSpace(x.Domain))
                .WithMessage("Domain can only contain lowercase letters, numbers, hyphens, and dots.");
        }
    }
}
