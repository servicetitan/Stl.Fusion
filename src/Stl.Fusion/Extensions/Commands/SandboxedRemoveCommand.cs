using System.Reactive;
using System.Runtime.Serialization;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Commands
{
    [DataContract]
    public record SandboxedRemoveCommand(
        [property: DataMember] Session Session,
        [property: DataMember] string Key
        ) : ISessionCommand<Unit>
    {
        public SandboxedRemoveCommand() : this(Session.Null, "") { }
    }
}
