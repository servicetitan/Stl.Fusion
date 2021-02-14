using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor
{
    public static class ComponentEx
    {
        private static readonly MethodInfo StateHasChangedMethod =
            typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static void StateHasChanges(ComponentBase component)
            => StateHasChangedMethod.Invoke(component, Array.Empty<object>());
    }
}
