using System;
using System.Reactive;
using System.Runtime.Serialization;
using Stl.CommandR.Commands;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    [DataContract]
    public record SetManyCommand(
        [property: DataMember] (string Key, string Value, Moment? ExpiresAt)[] Items
        ) : ServerSideCommandBase<Unit>
    {
        public SetManyCommand() : this(Array.Empty<(string, string, Moment?)>()) { }
    }
}
