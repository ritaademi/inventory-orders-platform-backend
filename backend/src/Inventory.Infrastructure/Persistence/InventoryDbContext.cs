using Inventory.Domain.Common;
using Inventory.Domain.Tenants;
using Inventory.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using Inventory.Domain.Auth;

namespace Inventory.Infrastructure.Persistence
{
    public class InventoryDbContext : DbContext
    {
        private readonly ITenantContext _tenant;

        public InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext tenant)
            : base(options)
        {
            _tenant = tenant;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Inventory.Domain.Users.User> Users => Set<Inventory.Domain.Users.User>();
        public DbSet<Inventory.Domain.Auth.Role> Roles => Set<Inventory.Domain.Auth.Role>();
        public DbSet<Inventory.Domain.Auth.UserRole> UserRoles => Set<Inventory.Domain.Auth.UserRole>();
        public DbSet<Inventory.Domain.Auth.RefreshToken> RefreshTokens => Set<Inventory.Domain.Auth.RefreshToken>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tenant>(b =>
            {
                b.ToTable("tenants");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Domain).HasMaxLength(200);
                b.Property(x => x.IsActive).HasDefaultValue(true);
            });

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                {
                    var param = Expression.Parameter(entityType.ClrType, "e");
                    var prop = Expression.Property(param, nameof(ITenantScoped.TenantId));
                    var getTenantId = Expression.Constant(this._tenant.TenantId, typeof(Guid?));

                    var tenantNull = Expression.Equal(getTenantId, Expression.Constant(null, typeof(Guid?)));
                    var equal = Expression.Equal(
                        prop,
                        Expression.Property(getTenantId, "Value")
                    );
                    var body = Expression.OrElse(tenantNull, equal);
                    var lambda = Expression.Lambda(body, param);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                    modelBuilder.ApplyConfiguration(new Inventory.Infrastructure.Persistence.Configurations.PermissionConfig());
                    modelBuilder.ApplyConfiguration(new Inventory.Infrastructure.Persistence.Configurations.RolePermissionConfig());
                }

                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    var p = Expression.Parameter(entityType.ClrType, "e");
                    var isDeletedProp = Expression.Property(p, nameof(ISoftDeletable.IsDeleted));
                    var notDeleted = Expression.Equal(isDeletedProp, Expression.Constant(false));
                    var lambda = Expression.Lambda(notDeleted, p);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
        
    }
}
