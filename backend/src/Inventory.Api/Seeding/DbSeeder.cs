using Inventory.Domain.Auth;
using Inventory.Domain.Tenants;
using Inventory.Domain.Users;
using Inventory.Infrastructure.Persistence;
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

    public DbSeeder(InventoryDbContext db, IPasswordHasher<User> hasher, ILogger<DbSeeder> log, IConfiguration cfg)
    {
        _db = db; _hasher = hasher; _log = log; _cfg = cfg;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var enabled = _cfg.GetValue("Seed:Enabled", defaultValue: true);
        if (!enabled)
        {
            _log.LogInformation("Seeding disabled via configuration.");
            return;
        }

        _log.LogInformation("Starting database seeding...");

        // 1) Ensure roles (global)
        await EnsureRolesAsync(ct);

        // 2) Ensure a demo tenant (dev)
        var tenantName = _cfg["Seed:TenantName"] ?? "Acme";
        var tenantDomain = _cfg["Seed:TenantDomain"] ?? "acme.local";
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Name == tenantName, ct);
        if (tenant is null)
        {
            tenant = new Tenant { Name = tenantName, Domain = tenantDomain, IsActive = true };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created tenant {Tenant} ({Domain})", tenantName, tenantDomain);
        }

        // 3) Ensure Owner user for that tenant (if none)
        var ownerEmail = (_cfg["Seed:OwnerEmail"] ?? "owner@acme.com").Trim().ToLowerInvariant();
        var ownerFullName = _cfg["Seed:OwnerFullName"] ?? "Acme Owner";
        var ownerPassword = _cfg["Seed:OwnerPassword"] ?? "Passw0rd!";

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == ownerEmail, ct);

        if (user is null)
        {
            user = new User
            {
                Email = ownerEmail,
                FullName = ownerFullName,
                IsActive = true,
                TenantId = tenant.Id
            };
            user.PasswordHash = _hasher.HashPassword(user, ownerPassword);
            _db.Users.Add(user);

            var ownerRole = await _db.Roles.SingleAsync(r => r.Name == "Owner", ct);
            _db.UserRoles.Add(new UserRole
            {
                User = user,
                Role = ownerRole,
                TenantId = tenant.Id
            });

            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created Owner user {Email} in tenant {Tenant}", ownerEmail, tenantName);
        }
        else
        {
            // ensure user has Owner role
            var hasOwner = user.UserRoles.Any(ur => ur.Role.Name == "Owner");
            if (!hasOwner)
            {
                var ownerRole = await _db.Roles.SingleAsync(r => r.Name == "Owner", ct);
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = ownerRole.Id, TenantId = tenant.Id });
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
        }
    }
}