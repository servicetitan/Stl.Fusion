using System;

namespace Stl.DependencyInjection
{
    public interface IHasServiceProvider
    {
        IServiceProvider ServiceProvider { get; }
    }
}
