using Inventory.Domain.Auth;
using Inventory.Domain.Common;
using Microsoft.VisualStudio.Services.UserAccountMapping;
using UserRole = Inventory.Domain.Auth.UserRole;


namespace Inventory.Domain.Users
{
    public class User : AuditableEntity, ITenantScoped
    {
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid TenantId { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
