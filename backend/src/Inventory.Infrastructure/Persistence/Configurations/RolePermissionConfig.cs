using Inventory.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    public class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> b)
        {
            b.ToTable("role_permissions");

            // Composite PK to prevent duplicates
            b.HasKey(x => new { x.RoleId, x.PermissionId });

            b.HasOne(x => x.Role)
             .WithMany(r => r.RolePermissions)
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}