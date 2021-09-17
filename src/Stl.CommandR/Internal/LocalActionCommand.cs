using System;
using System.Reactive;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Commands;

namespace Stl.CommandR.Internal
{
    public record LocalActionCommand : LocalCommand, ICommand<Unit>
    {
        [IgnoreDataMember, JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public Func<CancellationToken, Task>? Handler { get; init; }

        public override Task Run(CancellationToken cancellationToken)
        {
            if (Handler == null)
                throw Errors.LocalCommandHasNoHandler();
            return Handler.Invoke(cancellationToken);
        }
    }
}
