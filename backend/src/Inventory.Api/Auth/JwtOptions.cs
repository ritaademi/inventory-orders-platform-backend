namespace Inventory.Api.Auth;

public sealed class JwtOptions
{
    public string Key { get; set; } = default!;
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}