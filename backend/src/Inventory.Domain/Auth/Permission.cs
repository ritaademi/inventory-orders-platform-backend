using Inventory.Domain.Common;
using System.Collections.Generic;

namespace Inventory.Domain.Auth
{
    public class Permission : AuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
