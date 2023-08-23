namespace Stl.Multitenancy;

public interface ITenantResolver
{
    Task<Tenant> Resolve(object source, object context, CancellationToken cancellationToken);
}

public interface ITenantResolver<TContext> : ITenantResolver;
