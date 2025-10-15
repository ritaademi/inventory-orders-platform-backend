using Inventory.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations.Catalog;

public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.ToTable("product_variants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Sku).HasMaxLength(64);
        b.Property(x => x.Barcode).HasMaxLength(64);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.TenantId, x.ProductId });
        b.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique().HasFilter("\"Sku\" IS NOT NULL");
        b.HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique().HasFilter("\"Barcode\" IS NOT NULL");
    }
}   