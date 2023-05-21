using Stl.Fusion.Tests.Services;
using Stl.Fusion.UI;
using Stl.RegisterAttributes;

namespace Stl.Fusion.Tests.UIModels;

public class KeyValueModel<TValue>
{
    public string Key { get; set; } = "";
    public TValue Value { get; set; } = default!;
    public int UpdateCount { get; set; }
}

[RegisterService(typeof(IComputedState<KeyValueModel<string>>))]
public class StringKeyValueModelState : ComputedState<KeyValueModel<string>>
{
    private IMutableState<string> Locals { get; }

    private IKeyValueServiceClient<string> KeyValueServiceClient
        => Services.GetRequiredService<IKeyValueServiceClient<string>>();

    public StringKeyValueModelState(IServiceProvider services)
        : base(null!, services, false)
    {
        Locals = services.StateFactory().NewMutable("");
        Locals.AddEventHandler(StateEventKind.Updated, (_, _) => _ = this.Recompute());

        // ReSharper disable once VirtualMemberCallInConstructor
        Initialize(new Options() {
            UpdateDelayer = new UpdateDelayer(services.UIActionTracker(), 0.5),
            InitialValue = null!,
        });
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

    protected override Task UpdateCycle()
    {
        return base.UpdateCycle();
    }
}
