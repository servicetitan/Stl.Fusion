using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Extensibility;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Authentication.Internal
{
    [MatchFor(typeof(AuthSession), typeof(ArgumentHandlerProvider))]
    public class SessionArgumentHandler : EquatableArgumentHandler<AuthSession>
    {
        public new static SessionArgumentHandler Instance { get; } = new SessionArgumentHandler();

        protected SessionArgumentHandler()
        {
            PreprocessFunc = (method, invocation, index) => {
                var arguments = invocation.Arguments;
                if (!ReferenceEquals(arguments[index], null))
                    return;
                var services = invocation.InvocationTarget is IHasServiceProvider hsp
                    ? hsp.ServiceProvider
                    : method.Interceptor.ServiceProvider;
                var sessionAccessor = services.GetRequiredService<IAuthSessionAccessor>();
                arguments[index] = sessionAccessor.Session;
            };
        }
    }
}
