using Stl.Extensibility;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Authentication.Internal
{
    [MatchFor(typeof(AuthContext), typeof(ArgumentHandlerProvider))]
    public class AuthContextArgumentHandler : EquatableArgumentHandler<AuthContext>
    {
        public new static AuthContextArgumentHandler Instance { get; } = new AuthContextArgumentHandler();

        protected AuthContextArgumentHandler()
        {
            PreprocessFunc = (method, invocation, index) => {
                var arguments = invocation.Arguments;
                if (!ReferenceEquals(arguments[index], null))
                    return;
                arguments[index] = AuthContext.Current;
            };
        }
    }
}
