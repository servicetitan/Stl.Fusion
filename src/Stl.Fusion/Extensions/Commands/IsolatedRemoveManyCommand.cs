using System;
using System.Reactive;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    public record IsolatedRemoveManyCommand(Session Session, params string[] Keys) : ISessionCommand<Unit>
    {
        public IsolatedRemoveManyCommand() : this(Session.Null, Array.Empty<string>()) { }
    }
}
