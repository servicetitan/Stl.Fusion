using System;
using System.Reactive;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record IsolatedSetManyCommand(Session Session, (string Key, string Value, Moment? ExpiresAt)[] Items)
        : ISessionCommand<Unit>
    {
        public IsolatedSetManyCommand() : this(Session.Null, Array.Empty<(string, string, Moment?)>()) { }
    }
}
