using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Authentication
{
    public interface ISessionAccessor
    {
        Session? Session { get; set; }
    }

    [Service(typeof(ISessionAccessor), Lifetime = ServiceLifetime.Scoped)]
    public class SessionAccessor : ISessionAccessor
    {
        public Session? Session { get; set; }
    }
}
