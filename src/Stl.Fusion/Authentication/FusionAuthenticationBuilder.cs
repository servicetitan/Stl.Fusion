using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Authentication
{
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

        public FusionAuthenticationBuilder AddServerSideAuthService<TAuthService>()
            where TAuthService : class, IServerSideAuthService
            => AddServerSideAuthService(typeof(TAuthService));

        public FusionAuthenticationBuilder AddServerSideAuthService(Type? implementationType = null)
        {
            if (Services.Any(d => d.ServiceType == typeof(IServerSideAuthService)))
                return this;

            implementationType ??= typeof(InMemoryAuthService);
            var serverSideServiceType = typeof(IServerSideAuthService);
            if (!serverSideServiceType.IsAssignableFrom(implementationType))
                throw Errors.MustImplement(implementationType, serverSideServiceType, nameof(implementationType));

            Fusion.AddComputeService(serverSideServiceType, implementationType);
            Services.TryAddTransient<IAuthService>(c => c.GetRequiredService<IServerSideAuthService>());
            return this;
        }
    }
}
