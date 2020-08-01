using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;

namespace Stl.Fusion.UI
{
    [Serializable]
    public class LiveStateUpdaterAttribute : ServiceAttributeBase
    {
        public override void TryRegister(IServiceCollection services, Type implementationType)
        {
            var tLiveUpdater = typeof(ILiveStateUpdater);
            var tLiveUpdaterGeneric1 = typeof(ILiveStateUpdater<>);
            var tLiveUpdaterGeneric2 = typeof(ILiveStateUpdater<,>);
            if (implementationType.IsValueType)
                throw new ArgumentOutOfRangeException(nameof(implementationType));
            if (!tLiveUpdater.IsAssignableFrom(implementationType))
                throw Errors.MustImplement<ILiveStateUpdater>(implementationType, nameof(implementationType));

            foreach (var tImplemented in implementationType.GetInterfaces()) {
                if (!tImplemented.IsConstructedGenericType)
                    continue;
                if (!tLiveUpdater.IsAssignableFrom(tImplemented))
                    continue;
                var tBase = tImplemented.GetGenericTypeDefinition();
                var ga = tImplemented.GetGenericArguments();
                switch (ga.Length) {
                case 1:
                    if (tBase != tLiveUpdaterGeneric1)
                        continue;
                    services.AddLiveState(ga[0], implementationType);
                    break;
                case 2:
                    if (tBase != tLiveUpdaterGeneric2)
                        continue;
                    services.AddLiveState(ga[0], ga[1], implementationType);
                    break;
                default:
                    continue;
                }
            }
        }
    }
}
