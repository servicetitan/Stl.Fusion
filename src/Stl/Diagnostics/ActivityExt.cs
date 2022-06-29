using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Stl.Multitenancy;

namespace Stl.Diagnostics;

public static class ActivityExt
{
#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("activity")]
#endif
    public static Activity? AddTenantTags(this Activity? activity, Tenant tenant)
        => activity?.AddTag("tenantId", tenant.Id.Value);
}
