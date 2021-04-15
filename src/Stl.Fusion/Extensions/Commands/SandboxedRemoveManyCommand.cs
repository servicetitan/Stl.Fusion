using System;
using System.Reactive;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedRemoveManyCommand(Session Session, params string[] Keys) : ISessionCommand<Unit>
    {
        public SandboxedRemoveManyCommand() : this(Session.Null, Array.Empty<string>()) { }
    }
}
