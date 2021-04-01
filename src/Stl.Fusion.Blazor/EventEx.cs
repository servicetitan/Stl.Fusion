using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor
{
    // From https://gist.github.com/SteveSandersonMS/8a19d8e992f127bb2d2a315ec6c5a373
    // Addresses https://github.com/dotnet/aspnetcore/issues/18919
    public static class EventEx
    {
        // The repetition in here is because of the four combinations of handlers (sync/async * with/without arg)
        public static Action NonRenderingHandler(Action callback)
            => new SyncReceiver(callback).Invoke;
        public static Action<TValue> NonRenderingHandler<TValue>(Action<TValue> callback)
            => new SyncReceiver<TValue>(callback).Invoke;
        public static Func<Task> NonRenderingHandler(Func<Task> callback)
            => new AsyncReceiver(callback).Invoke;
        public static Func<TValue, Task> NonRenderingHandler<TValue>(Func<TValue, Task> callback)
            => new AsyncReceiver<TValue>(callback).Invoke;

        record SyncReceiver(Action Callback) : ReceiverBase { public void Invoke() => Callback(); }
        record SyncReceiver<T>(Action<T> Callback) : ReceiverBase { public void Invoke(T arg) => Callback(arg); }
        record AsyncReceiver(Func<Task> Callback) : ReceiverBase { public Task Invoke() => Callback(); }
        record AsyncReceiver<T>(Func<T, Task> Callback) : ReceiverBase { public Task Invoke(T arg) => Callback(arg); }

        // By implementing IHandleEvent, we can override the event handling logic on a per-handler basis
        // The logic here just calls the callback without triggering any re-rendering
        record ReceiverBase : IHandleEvent
        {
            public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
        }
    }
}
