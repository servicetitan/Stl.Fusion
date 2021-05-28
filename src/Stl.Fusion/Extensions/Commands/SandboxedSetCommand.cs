using System.Reactive;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedSetCommand(Session Session, string Key, string Value, Moment? ExpiresAt = null)
        : SessionCommandBase<Unit>(Session)
    {
        public SandboxedSetCommand() : this(Session.Null, "", "") { }
    }
}
