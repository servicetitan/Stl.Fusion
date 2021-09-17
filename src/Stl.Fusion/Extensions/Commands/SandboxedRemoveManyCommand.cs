using System;
using System.Reactive;
using System.Runtime.Serialization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    [DataContract]
    public record SandboxedRemoveManyCommand(
        [property: DataMember] Session Session,
        [property: DataMember] params string[] Keys
        ) : ISessionCommand<Unit>
    {
        public SandboxedRemoveManyCommand() : this(Session.Null, Array.Empty<string>()) { }
    }
}
