using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Collections;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;

namespace Stl.Tests.CommandR.Services
{
    [Service, AddCommandHandlers]
    public class MathService : ServiceBase, ICommandHandler<DivCommand, double>
    {
        public MathService(IServiceProvider services) : base(services) { }

        [CommandHandler(Priority = 2)]
        public Task<double> OnCommandAsync(DivCommand command, CommandContext<double> context, CancellationToken cancellationToken)
        {
            var handler = context.Handlers[^1];
            handler.GetType().Should().Be(typeof(MethodCommandHandler<DivCommand>));
            handler.Priority.Should().Be(2);

            Log.LogInformation($"{command.Divisible} / {command.Divisor} =");
            var result = command.Divisible / command.Divisor;
            Log.LogInformation($"  {result}");
            if (double.IsInfinity(result))
                throw new DivideByZeroException();
            return Task.FromResult(result);
        }

        [CommandHandler(Priority = 1)]
        public async Task<double> RecSumAsync(
            RecSumCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var typedContext = context.Cast<double>();
            var handler = context.Handlers[^1];
            handler.GetType().Should().Be(typeof(MethodCommandHandler<RecSumCommand>));
            handler.Priority.Should().Be(1);

            Log.LogInformation($"Arguments: {command.Arguments.ToDelimitedString()}");
            typedContext.Should().BeSameAs(CommandContext.GetCurrent());

            if (command.Arguments.Length == 0)
                return 0;

            var tailSum = await Services.CommandDispatcher().CallAsync(
                    new RecSumCommand() { Arguments = command.Arguments[1..] },
                    cancellationToken)
                .ConfigureAwait(false);
            return command.Arguments[0] + tailSum;
        }
    }
}
