using System;
using Stl.CommandR;
using System.Reactive;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedSetCommand(Session Session, string Key, string Value, Moment? ExpiresAt = null)
        : ISessionCommand<Unit>
    {
        #if NETSTANDARD2_0
        Type ICommand.ResultType => typeof(Unit);
        #endif
        
        public SandboxedSetCommand() : this(Session.Null, "", "") { }
    }
}
