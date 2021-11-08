using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Authentication;

public readonly struct FusionAuthenticationBuilder
{
    public FusionBuilder Fusion { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionAuthenticationBuilder(FusionBuilder fusion)
    {
        Fusion = fusion;

        Services.TryAddSingleton<ISessionFactory, SessionFactory>();
        Services.TryAddScoped<ISessionProvider, SessionProvider>();
        Services.TryAddTransient(c => (ISessionResolver) c.GetRequiredService<ISessionProvider>());
        Services.TryAddTransient(c => c.GetRequiredService<ISessionProvider>().Session);

        Services.TryAddSingleton<PresenceService.Options>();
        Services.TryAddScoped<PresenceService>();
    }

    public FusionAuthenticationBuilder AddAuthBackend<TAuthService>()
        where TAuthService : class, IAuthBackend
        => AddAuthBackend(typeof(TAuthService));

    public FusionAuthenticationBuilder AddAuthBackend(Type? implementationType = null)
    {
        if (Services.Any(d => d.ServiceType == typeof(IAuthBackend)))
            return this;

        implementationType ??= typeof(InMemoryAuthService);
        var serverSideServiceType = typeof(IAuthBackend);
        if (!serverSideServiceType.IsAssignableFrom(implementationType))
            throw Errors.MustImplement(implementationType, serverSideServiceType, nameof(implementationType));

        Fusion.AddComputeService(implementationType);
        Services.TryAddTransient(c => (IAuthBackend) c.GetRequiredService(implementationType));
        Services.TryAddTransient(c => (IAuth) c.GetRequiredService(implementationType));
        return this;
    }
}
