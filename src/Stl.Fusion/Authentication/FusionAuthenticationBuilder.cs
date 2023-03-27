using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Authentication;

public readonly struct FusionAuthenticationBuilder
{
    public FusionBuilder Fusion { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionAuthenticationBuilder(
        FusionBuilder fusion,
        Action<FusionAuthenticationBuilder>? configure)
    {
        Fusion = fusion;
        if (Services.HasService<ISessionFactory>()) {
            configure?.Invoke(this);
            return;
        }

        Services.TryAddScoped<ISessionProvider>(c => new SessionProvider(c));
        Services.TryAddScoped(c => (ISessionResolver) c.GetRequiredService<ISessionProvider>());
        Services.TryAddScoped(c => c.GetRequiredService<ISessionProvider>().Session);
        Services.TryAddSingleton<ISessionFactory>(_ => new SessionFactory());

        Services.TryAddSingleton(_ => new PresenceReporter.Options());
        Services.TryAddScoped(c => new PresenceReporter(
            c.GetRequiredService<PresenceReporter.Options>(), c));

        configure?.Invoke(this);
    }

    public FusionAuthenticationBuilder AddBackend<TAuthService>()
        where TAuthService : class, IAuthBackend
        => AddBackend(typeof(TAuthService));

    public FusionAuthenticationBuilder AddBackend(Type? implementationType = null)
    {
        if (Services.Any(d => d.ServiceType == typeof(IAuthBackend)))
            return this;

        implementationType ??= typeof(InMemoryAuthService);
        var serverSideServiceType = typeof(IAuthBackend);
        if (!serverSideServiceType.IsAssignableFrom(implementationType))
            throw Errors.MustImplement(implementationType, serverSideServiceType, nameof(implementationType));

        Fusion.AddComputeService(implementationType);
        Services.AddSingleton(c => (IAuthBackend) c.GetRequiredService(implementationType));
        Services.AddSingleton(c => (IAuth) c.GetRequiredService(implementationType));
        return this;
    }
}
