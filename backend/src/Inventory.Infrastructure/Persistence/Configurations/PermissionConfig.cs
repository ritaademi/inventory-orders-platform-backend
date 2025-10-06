using Inventory.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    public class PermissionConfig : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> b)
        {
            b.ToTable("permissions");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
             .HasMaxLength(150)
             .IsRequired();

            b.Property(x => x.Description)
             .HasMaxLength(500);

            // Global unique permission names
            b.HasIndex(x => x.Name).IsUnique();
        }
    }
}