using System;
using System.Reactive;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedSetManyCommand(Session Session, (string Key, string Value, Moment? ExpiresAt)[] Items)
        : SessionCommandBase<Unit>(Session)
    {
        public SandboxedSetManyCommand() : this(Session.Null, Array.Empty<(string, string, Moment?)>()) { }
    }
}
