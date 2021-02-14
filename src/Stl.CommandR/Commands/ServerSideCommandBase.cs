using System;
using System.Text.Json.Serialization;
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
        [JsonIgnore]
        [field: NonSerialized]
        public bool IsServerSide { get; set; }

        public virtual void Prepare(CommandContext context)
        {
            if (!IsServerSide)
                throw Errors.CommandIsServerSideOnly();
        }
    }
}
