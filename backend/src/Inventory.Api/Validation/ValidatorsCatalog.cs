using FluentValidation;
using Inventory.Api.Catalog;

namespace Inventory.Api.Validation;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
public class CreateUomValidator : AbstractValidator<CreateUomDto>
{
    public CreateUomValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Precision).InclusiveBetween(0, 6);
    }
}
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UomId).NotEmpty();
    }
}
public class CreateVariantValidator : AbstractValidator<CreateVariantDto>
{
    public CreateVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).MaximumLength(64);
        RuleFor(x => x.Barcode).MaximumLength(64);
    }
}