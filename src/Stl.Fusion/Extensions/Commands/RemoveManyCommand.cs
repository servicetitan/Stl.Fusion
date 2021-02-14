using System;
using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Extensions.Commands
{
    public record RemoveManyCommand(params string[] Keys) : ServerSideCommandBase<Unit>
    {
        public RemoveManyCommand() : this(Array.Empty<string>()) { }
    }
}
