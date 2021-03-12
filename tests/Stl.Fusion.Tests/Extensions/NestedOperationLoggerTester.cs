using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion.Extensions;

namespace Stl.Fusion.Tests.Extensions
{
    [ComputeService(Scope = ServiceScope.Services)]
    public class NestedOperationLoggerTester
    {
        public record SetManyCommand(string[] Keys, string ValuePrefix) : ICommand<Unit>
        {
            public SetManyCommand() : this(Array.Empty<string>(), "") { }
        }

        private IKeyValueStore KeyValueStore { get; }

        public NestedOperationLoggerTester(IKeyValueStore keyValueStore)
            => KeyValueStore = keyValueStore;

        [CommandHandler]
        public virtual async Task SetMany(SetManyCommand command, CancellationToken cancellationToken = default)
        {
            var (keys, valuePrefix) = command;
            var first = keys.FirstOrDefault();
            if (first == null)
                return;
            await KeyValueStore.Set(first, valuePrefix + keys.Length, cancellationToken);
            var nextCommand = new SetManyCommand(keys[1..], valuePrefix);
            await SetMany(nextCommand, cancellationToken).ConfigureAwait(false);
        }
    }
}
