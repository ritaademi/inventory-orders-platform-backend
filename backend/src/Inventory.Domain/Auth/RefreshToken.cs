using Inventory.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Auth
{
    public class RefreshToken : AuditableEntity, ITenantScoped
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }

        public string Token { get; set; } = default!;
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public Users.User User { get; set; } = default!;
        public bool IsActive => RevokedAt == null && DateTimeOffset.UtcNow < ExpiresAt;
    }
}
