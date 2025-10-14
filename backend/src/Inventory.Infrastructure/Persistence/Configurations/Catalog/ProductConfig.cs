using Inventory.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations.Catalog;

public class ProductConfig : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();   // unique SKU per tenant
        b.HasIndex(x => new { x.TenantId, x.Name });

        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.UomId);
    }
}