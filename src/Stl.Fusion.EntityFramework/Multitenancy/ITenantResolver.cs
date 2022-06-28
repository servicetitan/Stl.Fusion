using Stl.Fusion.Authentication;
using Stl.Multitenancy;

namespace Stl.Fusion.Multitenancy;

public interface ITenantResolver
{
    Task<Tenant> Resolve(Session session, CancellationToken cancellationToken = default);
    Task<Tenant> Resolve(ICommand command, CancellationToken cancellationToken = default);
}

public interface ITenantResolver<TContext> : ITenantResolver
{ }
