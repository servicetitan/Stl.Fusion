using Stl.Internal;

namespace Stl.Multitenancy;

public static class TenantRegistryExt
{
    public static Tenant Get(this ITenantRegistry tenantRegistry, Symbol tenantId)
        => tenantRegistry.TryGet(tenantId, out var tenant)
            ? tenant
            : throw Errors.TenantNotFound(tenantId);
}
