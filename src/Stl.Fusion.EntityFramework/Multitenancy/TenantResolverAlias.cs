using Stl.Fusion.Authentication;
using Stl.Multitenancy;

namespace Stl.Fusion.Multitenancy;

// Resolves tenants from any ITenantResolver to TContext
public class TenantResolverAlias<TContext> : ITenantResolver<TContext>
{
    private readonly ITenantResolver _source;

    public TenantResolverAlias(ITenantResolver source) 
        => _source = source;

    public Task<Tenant> Resolve(Session session, CancellationToken cancellationToken = default) 
        => _source.Resolve(session, cancellationToken);
    public Task<Tenant> Resolve(ICommand command, CancellationToken cancellationToken = default)
        => _source.Resolve(command, cancellationToken);
}

// Resolves tenants from ITenantResolver<TSourceContext> to TContext
public class TenantResolverAlias<TSourceContext, TContext> : TenantResolverAlias<TContext>
{
    public TenantResolverAlias(ITenantResolver<TSourceContext> source) : base(source) { }
}
