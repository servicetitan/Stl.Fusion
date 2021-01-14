using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion
{
    public class StateAttribute : ServiceAttributeBase
    {
        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddFusion().AddState(implementationType);
    }
}
