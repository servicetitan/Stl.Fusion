using System;

namespace Stl.DependencyInjection
{
    public interface IHasServices
    {
        IServiceProvider Services { get; }
    }
}
