using System;
using Stl.CommandR;
using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record SignOutCommand(Session Session, bool Force = false)
        : ISessionCommand<Unit>
    {
        #if NETSTANDARD2_0
        Type ICommand.ResultType => typeof(Unit);
        #endif
        
        public SignOutCommand() : this(Session.Null) { }
    }
}
