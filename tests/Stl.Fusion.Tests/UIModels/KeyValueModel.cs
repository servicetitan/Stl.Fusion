using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests.UIModels
{
    public class KeyValueModel<TValue>
    {
        public string Key { get; set; } = "";
        public TValue Value { get; set; } = default!;
        public int UpdateCount { get; set; }
    }

    [State]
    public class StringKeyValueModelState : LiveState<KeyValueModel<string>>
    {
        public new class Options : LiveState<KeyValueModel<string>>.Options
        {
            public Options()
            {
                UpdateDelayerFactory = _ => {
                    var options = new UpdateDelayer.Options() {
                        Delay = TimeSpan.FromSeconds(0.5),
                    };
                    return new UpdateDelayer(options);
                };
            }
        }

        protected IMutableState<string> Locals { get; }

        private IKeyValueServiceClient<string> KeyValueServiceClient
            => Services.GetRequiredService<IKeyValueServiceClient<string>>();

        public StringKeyValueModelState(Options options, IServiceProvider services, object? argument = null)
            : base(options, services, argument)
        {
            Locals = new MutableState<string>(services);
            Locals.AddEventHandler(StateEventKind.Updated, (s, e) => this.CancelUpdateDelay());
        }

        protected override async Task<KeyValueModel<string>> ComputeValueAsync(CancellationToken cancellationToken)
        {
            var updateCount = UnsafeValue?.UpdateCount ?? 0;
            var key = Locals.UnsafeValue ?? "";
            var value = await KeyValueServiceClient.GetAsync(key, cancellationToken).ConfigureAwait(false);
            return new KeyValueModel<string>() {
                Key = key,
                Value = value,
                UpdateCount = updateCount + 1,
            };
        }
    }
}
