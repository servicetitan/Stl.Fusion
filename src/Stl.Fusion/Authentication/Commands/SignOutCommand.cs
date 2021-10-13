using System.Reactive;
using System.Runtime.Serialization;

namespace Stl.Fusion.Authentication.Commands
{
    [DataContract]
    public record SignOutCommand(
        [property: DataMember] Session Session,
        [property: DataMember] bool Force = false
        ) : ISessionCommand<Unit>
    {
        public SignOutCommand() : this(Session.Null) { }
    }
}
