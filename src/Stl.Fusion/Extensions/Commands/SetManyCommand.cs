using System;
using System.Reactive;
using Stl.CommandR.Commands;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    public record SetManyCommand((string Key, string Value, Moment? ExpiresAt)[] Items) : ServerSideCommandBase<Unit>
    {
        public SetManyCommand() : this(Array.Empty<(string, string, Moment?)>()) { }
    }
}
