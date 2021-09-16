using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor
{
    // From https://gist.github.com/SteveSandersonMS/8a19d8e992f127bb2d2a315ec6c5a373
    // Addresses https://github.com/dotnet/aspnetcore/issues/18919
    public static class BlazorEventHelper
    {
        // By implementing IHandleEvent, we can override the event handling logic on a per-handler basis
        // The logic here just calls the callback without triggering any re-rendering
        record HandlerBase : IHandleEvent
        {
            public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
        }

        record SyncHandler(Action Handler) : HandlerBase { public void Invoke() => Handler(); }
        record SyncHandler<T>(Action<T> Handler) : HandlerBase { public void Invoke(T arg) => Handler(arg); }
        record AsyncHandler(Func<Task> Handler) : HandlerBase { public Task Invoke() => Handler(); }
        record AsyncHandler<T>(Func<T, Task> Handler) : HandlerBase { public Task Invoke(T arg) => Handler(arg); }

        public static Action NonRendering(Action handler)
            => new SyncHandler(handler).Invoke;
        public static Action<TValue> NonRendering<TValue>(Action<TValue> handler)
            => new SyncHandler<TValue>(handler).Invoke;
        public static Func<Task> NonRendering(Func<Task> handler)
            => new AsyncHandler(handler).Invoke;
        public static Func<TValue, Task> NonRendering<TValue>(Func<TValue, Task> handler)
            => new AsyncHandler<TValue>(handler).Invoke;

    }
}
