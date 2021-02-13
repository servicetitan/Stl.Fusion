using System.Reactive;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Extensions.Commands
{
    public record RemoveCommand(string Key) : ServerSideCommandBase<Unit>
    {
        public RemoveCommand() : this("") { }
    }
}
