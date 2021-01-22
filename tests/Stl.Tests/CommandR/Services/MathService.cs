using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Collections;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.CommandR.Internal;

namespace Stl.Tests.CommandR.Services
{
    [CommandService]
    public class MathService : ServiceBase
    {
        public MathService(IServiceProvider services) : base(services) { }

        [CommandHandler(Priority = 2)]
        protected virtual Task<double> DivAsync(DivCommand command, CancellationToken cancellationToken = default)
        {
            var context = CommandContext.GetCurrent<double>();
            var handler = context.ExecutionState.Handlers[^1];
            handler.GetType().Should().Be(typeof(MethodCommandHandler<DivCommand>));
            handler.Priority.Should().Be(2);

            Log.LogInformation("{Divisible} / {Divisor} =", command.Divisible, command.Divisor);
            var result = command.Divisible / command.Divisor;
            Log.LogInformation("  {Result}", result);
            if (double.IsInfinity(result))
                throw new DivideByZeroException();
            return Task.FromResult(result);
        }

        [CommandHandler(Priority = 1)]
        public virtual async Task<double> RecSumAsync(RecSumCommand command, CancellationToken cancellationToken = default)
        {
            var context = CommandContext.GetCurrent<double>();
            var handler = context.ExecutionState.Handlers[^1];
            handler.GetType().Should().Be(typeof(MethodCommandHandler<RecSumCommand>));
            handler.Priority.Should().Be(1);
            if (command.Isolate) {
                context.IsOutermost.Should().BeTrue();
                RecSumCommand.Tag.Value.Should().BeNull();
            }
            else {
                if (command.Arguments.Length == 1)
                    context.IsOutermost.Should().BeFalse();
                RecSumCommand.Tag.Value.Should().NotBeNull();
            }

            Log.LogInformation("Arguments: {Arguments}", command.Arguments.ToDelimitedString());

            if (command.Arguments.Length == 0)
                return 0;

            var tailCommand = new RecSumCommand() {
                Arguments = command.Arguments[1..],
                Isolate = command.Isolate,
            };
            var tailSumTask = command.Isolate
                ? context.Commander.CallAsync(tailCommand, command.Isolate, cancellationToken)
                : RecSumAsync(tailCommand, cancellationToken);
            var tailSum = await tailSumTask.ConfigureAwait(false);
            return command.Arguments[0] + tailSum;
        }
    }
}
