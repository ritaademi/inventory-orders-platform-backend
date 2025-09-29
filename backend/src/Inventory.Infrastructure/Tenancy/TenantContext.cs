using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Tenancy
{
    public interface ITenantContext
    {
        Guid? TenantId { get; }
        void Set(Guid tenantId);
    }

    public sealed class TenantContext : ITenantContext
    {
        public Guid? TenantId { get; private set; }
        public void Set(Guid tenantId) => TenantId = tenantId;
    }
}
