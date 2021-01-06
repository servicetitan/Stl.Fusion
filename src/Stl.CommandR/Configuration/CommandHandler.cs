using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR.Configuration
{
    public abstract record CommandHandler
    {
        public Type CommandType { get; }
        public double Order { get; }

        protected CommandHandler(Type commandType, double order = 0)
        {
            CommandType = commandType;
            Order = order;
        }

        public abstract Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public abstract record CommandHandler<TCommand> : CommandHandler
        where TCommand : class, ICommand
    {
        protected CommandHandler(double order = 0) : base(typeof(TCommand), order) { }
    }
}
