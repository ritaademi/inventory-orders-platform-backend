namespace Inventory.Api.Catalog;

public record CategoryDto(Guid Id, string Name, Guid? ParentId);
public record CreateCategoryDto(string Name, Guid? ParentId);

public record UomDto(Guid Id, string Code, string Name, int Precision);
public record CreateUomDto(string Code, string Name, int Precision);

public record ProductDto(Guid Id, string Sku, string Name, string? Description, Guid? CategoryId, Guid UomId, bool IsActive);
public record CreateProductDto(string Sku, string Name, string? Description, Guid? CategoryId, Guid UomId, bool IsActive);

public record VariantDto(Guid Id, Guid ProductId, string? Sku, string? Barcode, string? AttributesJson, bool IsActive);
public record CreateVariantDto(Guid ProductId, string? Sku, string? Barcode, string? AttributesJson, bool IsActive);

public record ListQuery(int Page = 1, int Size = 10, string? Search = null, string? Sort = null);