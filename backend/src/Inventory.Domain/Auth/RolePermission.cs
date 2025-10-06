using Inventory.Domain.Common;

namespace Inventory.Domain.Auth
{
    public class RolePermission : AuditableEntity
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public Role Role { get; set; } = default!;
        public Permission Permission { get; set; } = default!;
    }
}
