using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Commands
{
    public interface IServerSideCommand : IPreparedCommand
    {
        bool IsServerSide { get; set; }
    }

    public interface IServerSideCommand<TResult> : IServerSideCommand, ICommand<TResult>
    { }

    public abstract record ServerSideCommandBase<TResult> : IServerSideCommand<TResult>
    {
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        [field: NonSerialized]
        public bool IsServerSide { get; set; }

        public virtual Task Prepare(CommandContext context, CancellationToken cancellationToken)
        {
            if (!IsServerSide)
                throw Errors.CommandIsServerSideOnly();
            return Task.CompletedTask;
        }
    }
}
