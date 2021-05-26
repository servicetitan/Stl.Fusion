using Stl.CommandR;
using System;
using System.Reactive;

namespace Stl.Fusion.Authentication.Commands
{
    public record EditUserCommand(Session Session, string? Name = null)
        : ISessionCommand<Unit>
    {
        #if NETSTANDARD2_0
        Type ICommand.ResultType => typeof(Unit);
        #endif
        
        public EditUserCommand() : this(Session.Null) { }
    }
}
