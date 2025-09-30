using Inventory.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Auth
{
    public class UserRole : AuditableEntity, ITenantScoped
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid TenantId { get; set; }

        public Users.User User { get; set; } = default!;
        public Role Role { get; set; } = default!;
    }
}
