using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.UI;

namespace Stl.Fusion.Tests.UIModels
{
    public class KeyValueModel<TValue>
    {
        public string Key { get; set; } = "";
        public TValue Value { get; set; } = default!;
        public int UpdateCount { get; set; }
    }

    [LiveStateUpdater]
    public class StringKeyValueModelUpdater : ILiveStateUpdater<string, KeyValueModel<string>>
    {
        private IKeyValueServiceClient<string> KeyValueServiceClient { get; }

        public StringKeyValueModelUpdater(IKeyValueServiceClient<string> service)
            => KeyValueServiceClient = service;

        public async Task<KeyValueModel<string>> UpdateAsync(ILiveState<string, KeyValueModel<string>> liveState, CancellationToken cancellationToken)
        {
            var updateCount = liveState.UnsafeValue?.UpdateCount ?? 0;
            var key = liveState.Local ?? "";
            var value = await KeyValueServiceClient.GetAsync(key, cancellationToken).ConfigureAwait(false);
            return new KeyValueModel<string>() {
                Key = key,
                Value = value,
                UpdateCount = updateCount + 1,
            };
        }
    }
}
