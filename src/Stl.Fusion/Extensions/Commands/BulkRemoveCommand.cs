using System;
using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Extensions.Commands
{
    public record BulkRemoveCommand(params string[] Keys) : ServerSideCommandBase<Unit>
    {
        public BulkRemoveCommand() : this(Array.Empty<string>()) { }
    }
}
