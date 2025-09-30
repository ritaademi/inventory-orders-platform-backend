using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventory.Domain.Users;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(200);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

            b.HasMany(x => x.UserRoles)
             .WithOne(ur => ur.User)
             .HasForeignKey(ur => ur.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.RefreshTokens)
             .WithOne(rt => rt.User)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
