namespace Inventory.Api.Tenants
{
    public sealed record CreateTenantRequest(string Name, string? Domain);
}
