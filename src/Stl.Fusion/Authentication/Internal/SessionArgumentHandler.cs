using Stl.Extensibility;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Authentication.Internal
{
    [MatchFor(typeof(Session), typeof(ArgumentHandlerProvider))]
    public class SessionArgumentHandler : EquatableArgumentHandler<Session>
    {
        public new static SessionArgumentHandler Instance { get; } = new SessionArgumentHandler();

        protected SessionArgumentHandler()
        {
            PreprocessFunc = (method, invocation, index) => {
                var arguments = invocation.Arguments;
                if (!ReferenceEquals(arguments[index], null))
                    return;
                arguments[index] = Session.Current;
            };
        }
    }
}
