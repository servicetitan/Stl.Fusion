namespace Stl.Multitenancy;

// Resolves tenants from any ITenantResolver to TContext
public class TenantResolverAlias<TContext>(ITenantResolver source) : ITenantResolver<TContext>
{
    private readonly ITenantResolver _source = source;

    public Task<Tenant> Resolve(object source, object context, CancellationToken cancellationToken)
        => _source.Resolve(source, context, cancellationToken);
}

// Resolves tenants from ITenantResolver<TSourceContext> to TContext
public class TenantResolverAlias<TSourceContext, TContext>(ITenantResolver<TSourceContext> source)
    : TenantResolverAlias<TContext>(source);
