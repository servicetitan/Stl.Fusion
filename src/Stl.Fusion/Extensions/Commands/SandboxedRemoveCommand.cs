using System;
using Stl.CommandR;
using System.Reactive;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedRemoveCommand(Session Session, string Key) : ISessionCommand<Unit>
    {
        #if NETSTANDARD2_0
        Type ICommand.ResultType => typeof(Unit);
        #endif
        
        public SandboxedRemoveCommand() : this(Session.Null, "") { }
    }
}
