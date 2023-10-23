using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor;

public readonly struct FusionBlazorBuilder
{
    private class AddedTag;
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public FusionBuilder Fusion { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionBlazorBuilder(
        FusionBuilder fusion,
        Action<FusionBlazorBuilder>? configure)
    {
        Fusion = fusion;
        var services = Services;
        if (services.Contains(AddedTagDescriptor)) {
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);
        services.TryAddScoped(c => new UICommander(c));
        services.TryAddScoped(_ => new UIActionFailureTracker.Options());
        services.TryAddScoped(c => new UIActionFailureTracker(
            c.GetRequiredService<UIActionFailureTracker.Options>(), c));
        services.TryAddScoped(c => new BlazorModeHelper(
            c.GetRequiredService<NavigationManager>()));
        services.TryAddScoped(c => new BlazorCircuitContext(c));
    }
}
