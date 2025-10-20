namespace Inventory.Api.Models.Requests
{
    public sealed class SupplierUpsertRequest
    {
        public required string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public sealed class CustomerUpsertRequest
    {
        public required string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}
