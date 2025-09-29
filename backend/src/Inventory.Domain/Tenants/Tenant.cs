using Inventory.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Tenants
{
    public class Tenant : AuditableEntity
    {
        public string Name { get; set; } = default!;
        public string? Domain { get; set; }
        public bool IsActive { get; set; } = true;
        public string? SettingsJson { get; set; }
    }
}
