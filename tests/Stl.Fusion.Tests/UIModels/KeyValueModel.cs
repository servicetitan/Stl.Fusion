using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests.UIModels
{
    public class KeyValueModel<TValue>
    {
        public string Key { get; set; } = "";
        public TValue Value { get; set; } = default!;
        public int UpdateCount { get; set; }
    }

    [Service(typeof(IComputedState<KeyValueModel<string>>))]
    public class StringKeyValueModelState : ComputedState<KeyValueModel<string>>
    {
        protected IMutableState<string> Locals { get; }

        private IKeyValueServiceClient<string> KeyValueServiceClient
            => Services.GetRequiredService<IKeyValueServiceClient<string>>();

        public StringKeyValueModelState(IServiceProvider services)
            : base(
                new Options() { UpdateDelayer = new UpdateDelayer(0.5) },
                services)
        {
            Locals = new MutableState<string>(services);
            Locals.AddEventHandler(StateEventKind.Updated, (_, _) => this.Recompute());
        }

        protected override async Task<KeyValueModel<string>> Compute(CancellationToken cancellationToken)
        {
            var updateCount = ValueOrDefault?.UpdateCount ?? 0;
            var key = Locals.ValueOrDefault ?? "";
            var value = await KeyValueServiceClient.Get(key, cancellationToken).ConfigureAwait(false);
            return new KeyValueModel<string>() {
                Key = key,
                Value = value,
                UpdateCount = updateCount + 1,
            };
        }
    }
}
