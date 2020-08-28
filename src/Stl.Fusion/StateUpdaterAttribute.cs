using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion
{
    [Serializable]
    public class StateAttribute : ServiceAttributeBase
    {
        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddState(implementationType);
    }
}
