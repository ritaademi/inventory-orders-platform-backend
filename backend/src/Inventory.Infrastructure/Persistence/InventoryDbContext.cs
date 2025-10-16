using System.Linq.Expressions;
using Inventory.Application.Multitenancy;
using Inventory.Domain.Auth;
using Inventory.Domain.Catalog;
using Inventory.Domain.Common;
using Inventory.Domain.Tenants;
using Inventory.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext tenant)
        : base(options) => _tenant = tenant;

    // ========= DbSets =========
    // Tenants
    public DbSet<Tenant> Tenants => Set<Tenant>();

    // Auth / Users
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UnitOfMeasure> Uoms => Set<UnitOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply IEntityTypeConfiguration<> from this assembly (Infrastructure)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        // Tenants table config
        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Domain).HasMaxLength(200);
            b.Property(x => x.IsActive).HasDefaultValue(true);
        });

        // Global query filters: Tenant + SoftDelete (combined per-entity)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;

            var isTenantScoped = typeof(ITenantScoped).IsAssignableFrom(clr);
            var isSoftDelete   = typeof(ISoftDeletable).IsAssignableFrom(clr);

            if (!isTenantScoped && !isSoftDelete) continue;

            var param = Expression.Parameter(clr, "e");
            Expression body = Expression.Constant(true);

            if (isTenantScoped)
            {
                // (tenant == null) || (e.TenantId == tenant.Value)
                var tenantIdProp = Expression.Property(param, nameof(ITenantScoped.TenantId));
                var ctxTenantId  = Expression.Constant(_tenant.TenantId, typeof(Guid?));
                var tenantNull   = Expression.Equal(ctxTenantId, Expression.Constant(null, typeof(Guid?)));
                var tenantEq     = Expression.Equal(tenantIdProp, Expression.Property(ctxTenantId, "Value"));
                body = Expression.AndAlso(body, Expression.OrElse(tenantNull, tenantEq));
            }

            if (isSoftDelete)
            {
                // e.IsDeleted == false
                var isDeletedProp = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
                var notDeleted    = Expression.Equal(isDeletedProp, Expression.Constant(false));
                body = Expression.AndAlso(body, notDeleted);
            }

            var lambda = Expression.Lambda(body, param);
            modelBuilder.Entity(clr).HasQueryFilter(lambda);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // audit timestamps
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        // set TenantId automatically on Added/Modified for tenant-scoped entities
        if (_tenant.TenantId.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is ITenantScoped scoped &&
                    (entry.State == EntityState.Added || entry.State == EntityState.Modified))
                {
                    scoped.TenantId = _tenant.TenantId.Value;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
