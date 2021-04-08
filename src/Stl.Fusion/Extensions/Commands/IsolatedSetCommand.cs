using System.Reactive;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record IsolatedSetCommand(Session Session, string Key, string Value, Moment? ExpiresAt = null)
        : ISessionCommand<Unit>
    {
        public IsolatedSetCommand() : this(Session.Null, "", "") { }
    }
}
