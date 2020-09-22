using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Authentication
{
    public readonly struct FusionAuthenticationBuilder
    {
        public FusionBuilder Fusion { get; }
        public IServiceCollection Services => Fusion.Services;

        internal FusionAuthenticationBuilder(FusionBuilder fusion)
        {
            Fusion = fusion;
            Services.TryAddScoped<AuthContextAccessor>();
            Services.TryAddScoped(c => c.GetRequiredService<AuthContextAccessor>().Context.AssertNotNull());
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
            Services.TryAddSingleton<IAuthService>(c => c.GetRequiredService<IServerSideAuthService>());
            return this;
        }
    }
}
