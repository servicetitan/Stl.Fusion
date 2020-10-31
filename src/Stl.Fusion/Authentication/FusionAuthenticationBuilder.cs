using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Internal;

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

        public FusionBuilder BackToFusion() => Fusion;
        public IServiceCollection BackToServices() => Services;

        public FusionAuthenticationBuilder AddAuthService<TAuthService>()
            where TAuthService : class, IServerSideAuthService
            => AddAuthService(typeof(TAuthService));

        public FusionAuthenticationBuilder AddAuthService(Type? implementationType = null)
        {
            implementationType ??= typeof(InProcessAuthService);
            var serverSideServiceType = typeof(IServerSideAuthService);
            if (!serverSideServiceType.IsAssignableFrom(implementationType))
                throw Errors.MustImplement(implementationType, serverSideServiceType, nameof(implementationType));

            Fusion.AddComputeService(serverSideServiceType, implementationType);
            Services.TryAddTransient<IAuthService>(c => c.GetRequiredService<IServerSideAuthService>());
            return this;
        }
    }
}
