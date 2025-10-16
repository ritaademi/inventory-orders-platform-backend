using Inventory.Domain.Auth;
using Inventory.Domain.Tenants;
using Inventory.Domain.Users;
using Inventory.Infrastructure.Persistence;
using Inventory.Application.Multitenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Seeding;

public sealed class DbSeeder
{
    private static readonly string[] BuiltInRoles = { "Owner", "Admin", "Manager", "Clerk", "Viewer" };

    private readonly InventoryDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<DbSeeder> _log;
    private readonly IConfiguration _cfg;
    private readonly ITenantContext _tenantCtx;

    public DbSeeder(
        InventoryDbContext db,
        IPasswordHasher<User> hasher,
        ILogger<DbSeeder> log,
        IConfiguration cfg,
        ITenantContext tenantCtx)
    {
        _db = db;
        _hasher = hasher;
        _log = log;
        _cfg = cfg;
        _tenantCtx = tenantCtx;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var enabled = _cfg.GetValue("Seed:Enabled", true);
        if (!enabled)
        {
            _log.LogInformation("Seeding disabled via configuration.");
            return;
        }

        _log.LogInformation("Starting database seeding...");

        // 1) Roles (globale, jo-tenant scoped)
        await EnsureRolesAsync(ct);

        // 2) Tenant default (dev/local)
        var tenantName   = _cfg["Seed:TenantName"]   ?? "Acme";
        var tenantDomain = _cfg["Seed:TenantDomain"] ?? "acme.local";

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Name == tenantName, ct);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = tenantName.Trim(),
                Domain = tenantDomain.Trim(),
                IsActive = true
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created tenant {Tenant} ({Domain})", tenant.Name, tenant.Domain);
        }
        else
        {
            if (!tenant.IsActive)
            {
                tenant.IsActive = true;
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Reactivated tenant {Tenant}", tenant.Name);
            }
        }

        // Vendos tenant-in në kontekst që interceptor-i të plotësojë TenantId automatikisht
        _tenantCtx.Set(tenant.Id);

        // 3) Owner user për tenant-in
        var ownerEmail    = (_cfg["Seed:OwnerEmail"]    ?? "owner@acme.com").Trim().ToLowerInvariant();
        var ownerFullName =  _cfg["Seed:OwnerFullName"] ?? "Acme Owner";
        var ownerPassword =  _cfg["Seed:OwnerPassword"] ?? "Passw0rd!";

        // përdorim query me tenant filter aktiv (falë _tenantCtx)
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == ownerEmail, ct);

        if (user is null)
        {
            user = new User
            {
                Email    = ownerEmail,
                FullName = ownerFullName,
                IsActive = true
                // TenantId do të vendoset nga interceptor-i (falë _tenantCtx)
            };

            user.PasswordHash = _hasher.HashPassword(user, ownerPassword);
            _db.Users.Add(user);

            var ownerRole = await _db.Roles.SingleAsync(r => r.Name == "Owner", ct);

            // TenantId do të vendoset nga interceptor-i
            _db.UserRoles.Add(new UserRole
            {
                User  = user,
                Role  = ownerRole
                // TenantId set nga interceptor
            });

            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created Owner user {Email} in tenant {Tenant}", ownerEmail, tenant.Name);
        }
        else
        {
            // siguro rolin Owner
            var hasOwner = user.UserRoles.Any(ur => ur.Role.Name == "Owner");
            if (!hasOwner)
            {
                var ownerRole = await _db.Roles.SingleAsync(r => r.Name == "Owner", ct);

                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = ownerRole.Id
                    // TenantId set nga interceptor
                });

                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Granted Owner role to existing user {Email}", ownerEmail);
            }
        }

        _log.LogInformation("Seeding completed.");
    }

    private async Task EnsureRolesAsync(CancellationToken ct)
    {
        var existing = await _db.Roles.Select(r => r.Name).ToListAsync(ct);

        var toAdd = BuiltInRoles
            .Except(existing, StringComparer.OrdinalIgnoreCase)
            .Select(n => new Role { Name = n })
            .ToList();

        if (toAdd.Count > 0)
        {
            _db.Roles.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Seeded roles: {Roles}", string.Join(", ", toAdd.Select(r => r.Name)));
        }
    }
}
