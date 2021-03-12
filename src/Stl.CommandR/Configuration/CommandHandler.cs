using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR.Configuration
{
    public abstract record CommandHandler
    {
        public Type CommandType { get; }
        public bool IsFilter { get; }
        public double Priority { get; }

        protected CommandHandler(Type commandType, bool isFilter = false, double priority = 0)
        {
            CommandType = commandType;
            IsFilter = isFilter;
            Priority = priority;
        }

        public abstract object GetHandlerService(
            ICommand command, CommandContext context);

        public abstract Task Invoke(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public abstract record CommandHandler<TCommand> : CommandHandler
        where TCommand : class, ICommand
    {
        protected CommandHandler(bool isFilter = false, double priority = 0)
            : base(typeof(TCommand), isFilter, priority) { }
    }
}
