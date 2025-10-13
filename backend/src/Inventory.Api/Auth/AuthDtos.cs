namespace Inventory.Api.Auth;

public record RegisterDto(string Email, string Password, string? FullName);
public record LoginDto(string Email, string Password);
public record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);
public record RefreshRequest(string RefreshToken);