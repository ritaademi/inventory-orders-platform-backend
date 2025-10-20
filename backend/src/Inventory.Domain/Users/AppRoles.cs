namespace Inventory.Domain.Users;

public static class AppRoles
{
    public const string Owner   = "Owner";   // ekziston në policies, e ruajmë
    public const string Admin   = "Admin";
    public const string Manager = "Manager";
    public const string Clerk   = "Clerk";

    public static readonly string[] All =
    {
        Owner, Admin, Manager, Clerk
    };
}
