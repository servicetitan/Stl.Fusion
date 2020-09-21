using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Authentication
{
    public interface IAuthSessionAccessor
    {
        AuthSession? Session { get; set; }
    }

    [Service(typeof(IAuthSessionAccessor), Lifetime = ServiceLifetime.Scoped)]
    public class AuthSessionAccessor : IAuthSessionAccessor
    {
        public AuthSession? Session { get; set; }
    }
}
