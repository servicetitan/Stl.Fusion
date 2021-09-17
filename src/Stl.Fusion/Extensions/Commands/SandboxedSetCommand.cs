using System.Reactive;
using System.Runtime.Serialization;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Commands
{
    [DataContract]
    public record SandboxedSetCommand(
        [property: DataMember] Session Session,
        [property: DataMember] string Key,
        [property: DataMember] string Value,
        [property: DataMember] Moment? ExpiresAt = null
        ) : ISessionCommand<Unit>
    {
        public SandboxedSetCommand() : this(Session.Null, "", "") { }
    }
}
