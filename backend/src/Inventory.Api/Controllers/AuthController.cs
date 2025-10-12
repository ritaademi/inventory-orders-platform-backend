using System.Security.Claims;
using Inventory.Api.Auth;
using Inventory.Domain.Auth;
using Inventory.Domain.Users;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly InventoryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IJwtTokenService _tokens;
    private readonly IPasswordHasher<User> _hasher;

    private static readonly string[] BuiltInRoles = new[] { "Owner", "Admin", "Manager", "Clerk", "Viewer" };

    public AuthController(
        InventoryDbContext db,
        ITenantContext tenant,
        IJwtTokenService tokens,
        IPasswordHasher<User> hasher)
    {
        _db = db; _tenant = tenant; _tokens = tokens; _hasher = hasher;
    }

    // POST /auth/register  (first user per tenant becomes Owner)
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        if (_tenant.TenantId is null) return BadRequest("Missing X-Tenant-Id header.");

        // If users exist already, block (later: only Owner/Admin can invite)
        var hasUsers = await _db.Users.AnyAsync(u => u.TenantId == _tenant.TenantId, ct);
        if (hasUsers) return Forbid("Tenant already initialized. Use admin invitation flow.");

        await EnsureRolesAsync(ct);

        var user = new User
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            FullName = dto.FullName,
            TenantId = _tenant.TenantId!.Value,
            IsActive = true
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        // Assign Owner
        var owner = await _db.Roles.SingleAsync(r => r.Name == "Owner", ct);
        _db.Users.Add(user);
        _db.UserRoles.Add(new UserRole { User = user, Role = owner, TenantId = user.TenantId });

        // Persist + issue tokens
        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            User = user,
            Token = _tokens.CreateRefreshToken(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        var roles = new[] { "Owner" };
        var access = _tokens.CreateAccessToken(user, user.TenantId, roles, out var exp);
        return Ok(new TokenResponse(access, refresh.Token, exp));
    }

    // POST /auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        if (_tenant.TenantId is null) return BadRequest("Missing X-Tenant-Id header.");

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.TenantId == _tenant.TenantId && u.Email == dto.Email.ToLower(), ct);

        if (user is null || !user.IsActive)
            return Unauthorized("Invalid credentials.");

        var res = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (res == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid credentials.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();

        // rotate refresh token (simple strategy)
        var oldTokens = _db.RefreshTokens.Where(t => t.UserId == user.Id && t.TenantId == user.TenantId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow);
        foreach (var t in oldTokens) t.RevokedAt = DateTimeOffset.UtcNow;

        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = _tokens.CreateRefreshToken(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        var access = _tokens.CreateAccessToken(user, user.TenantId, roles, out var exp);
        return Ok(new TokenResponse(access, refresh.Token, exp));
    }

    // POST /auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
    {
        if (_tenant.TenantId is null) return BadRequest("Missing X-Tenant-Id header.");
        var token = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(t => t.Token == body.RefreshToken && t.TenantId == _tenant.TenantId, ct);

        if (token is null || token.RevokedAt != null || token.ExpiresAt <= DateTimeOffset.UtcNow)
            return Unauthorized("Invalid refresh token.");

        // rotate
        token.RevokedAt = DateTimeOffset.UtcNow;
        var newRefresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = token.TenantId,
            UserId = token.UserId,
            Token = _tokens.CreateRefreshToken(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(newRefresh);

        var roles = token.User.UserRoles.Select(ur => ur.Role.Name);
        var access = _tokens.CreateAccessToken(token.User, token.TenantId, roles, out var exp);
        await _db.SaveChangesAsync(ct);

        return Ok(new TokenResponse(access, newRefresh.Token, exp));
    }

    // POST /auth/logout  (revoke all active refresh tokens for current user)
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userId, out var uid)) return Unauthorized();

        if (_tenant.TenantId is null) return BadRequest("Missing X-Tenant-Id header.");

        var tokens = _db.RefreshTokens.Where(t => t.UserId == uid && t.TenantId == _tenant.TenantId && t.RevokedAt == null);
        await tokens.ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow), ct);
        return NoContent();
    }

    private async Task EnsureRolesAsync(CancellationToken ct)
    {
        var existing = await _db.Roles.Select(r => r.Name).ToListAsync(ct);
        var toAdd = BuiltInRoles.Except(existing, StringComparer.OrdinalIgnoreCase)
            .Select(n => new Role { Name = n });
        if (toAdd.Any()) _db.Roles.AddRange(toAdd);
    }
}