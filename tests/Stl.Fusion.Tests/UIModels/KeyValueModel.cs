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
    public class StringKeyValueModelState : LiveState<KeyValueModel<string>, string>
    {
        public new class Options : LiveState<KeyValueModel<string>, string>.Options
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

        private IKeyValueServiceClient<string> KeyValueServiceClient
            => ServiceProvider.GetRequiredService<IKeyValueServiceClient<string>>();

        public StringKeyValueModelState(Options options, IServiceProvider serviceProvider, object? argument = null)
            : base(options, serviceProvider, argument) { }

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
