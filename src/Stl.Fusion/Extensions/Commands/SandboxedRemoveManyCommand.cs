using System;
using System.Reactive;
using Stl.CommandR;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedRemoveManyCommand(Session Session, params string[] Keys) : ISessionCommand<Unit>
    {
        #if NETSTANDARD2_0
        Type ICommand.ResultType => typeof(Unit);
        #endif
        
        public SandboxedRemoveManyCommand() : this(Session.Null, Array.Empty<string>()) { }
    }
}
