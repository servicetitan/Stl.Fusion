using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Stl.Fusion.Blazor;

public static class DispatcherExt
{
    private static readonly Dispatcher? NullDispatcher = null;

    static DispatcherExt()
    {
        var assembly = typeof(WebAssemblyHost).Assembly;
        var tNullDispatcher = assembly.GetType("Microsoft.AspNetCore.Components.WebAssembly.Rendering.NullDispatcher");
        var fInstance = tNullDispatcher?.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        NullDispatcher = (Dispatcher?)fInstance?.GetValue(null);
    }

    public static bool IsNullDispatcher(this Dispatcher dispatcher)
        => ReferenceEquals(dispatcher, NullDispatcher);
}
