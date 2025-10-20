namespace Inventory.Api.Auth;

public static class PolicyNames
{
    public const string RequireTenant   = "RequireTenant";
    public const string AdminOnly       = "AdminOnly";
    public const string ManageInventory = "ManageInventory";
    public const string ManageOrders    = "ManageOrders";
}
