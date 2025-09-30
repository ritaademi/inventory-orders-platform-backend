using Inventory.Domain.Auth;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.ToTable("refresh_tokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.Token).HasMaxLength(500).IsRequired();
            b.Property(x => x.ExpiresAt).IsRequired();

            b.HasIndex(x => x.Token).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.UserId });
        }
    }
}
