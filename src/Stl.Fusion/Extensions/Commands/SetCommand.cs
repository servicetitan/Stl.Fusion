using System.Reactive;
using Stl.CommandR.Commands;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record SetCommand(string Key, string Value, Moment? ExpiresAt = null) : ServerSideCommandBase<Unit>
    {
        public SetCommand() : this("", "") { }
    }
}
