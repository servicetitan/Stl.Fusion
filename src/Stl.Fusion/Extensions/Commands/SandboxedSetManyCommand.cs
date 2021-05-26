using System;
using System.Reactive;
using Stl.CommandR;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record SandboxedSetManyCommand(Session Session, (string Key, string Value, Moment? ExpiresAt)[] Items)
        : ISessionCommand<Unit>
    {
        #if NETSTANDARD2_0
        Type ICommand.ResultType => typeof(Unit);
        #endif
        
        public SandboxedSetManyCommand() : this(Session.Null, Array.Empty<(string, string, Moment?)>()) { }
    }
}
