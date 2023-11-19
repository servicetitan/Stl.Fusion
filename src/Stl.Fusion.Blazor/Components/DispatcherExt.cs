using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Stl.Internal;

namespace Stl.Fusion.Blazor;

#if !NET5_0
[RequiresUnreferencedCode(UnreferencedCode.Reflection)]
#endif
public static class DispatcherExt
{
    private static readonly Dispatcher? NullDispatcher = null;

    static DispatcherExt()
    {
        var assembly = typeof(WebAssemblyHost).Assembly;
#pragma warning disable IL2026, IL2075
        var tNullDispatcher = assembly.GetType("Microsoft.AspNetCore.Components.WebAssembly.Rendering.NullDispatcher");
        var fInstance = tNullDispatcher?.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
#pragma warning restore IL2026, IL2075
        NullDispatcher = (Dispatcher?)fInstance?.GetValue(null);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static bool IsNullDispatcher(this Dispatcher dispatcher)
        => ReferenceEquals(dispatcher, NullDispatcher);
}
