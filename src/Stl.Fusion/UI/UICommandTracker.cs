using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;
using Stl.Time;

namespace Stl.Fusion.UI
{
    public interface IUICommandTracker : IDisposable
    {
        MomentClockSet Clocks { get; }
        IAsyncEnumerable<UICommandEvent> Events { get; }

        UICommandEvent? LastEvent { get; }
        UICommandEvent? LastCommandStarted { get; }
        UICommandEvent? LastCommandCompleted { get; }
        UICommandEvent? LastCommandFailed { get; }

        Task<UICommandEvent> WhenAnyEvent();
        Task<UICommandEvent> WhenCommandStarted();
        Task<UICommandEvent> WhenCommandCompleted();
        Task<UICommandEvent> WhenCommandFailed();

        UICommandEvent ProcessEvent(UICommandEvent commandEvent);
    }

    public class UICommandTracker : IUICommandTracker
    {
        private class NoUICommandTracker : UICommandTracker
        {
            public override UICommandEvent ProcessEvent(UICommandEvent commandEvent)
                => throw Errors.InternalError($"{GetType().Name}.{nameof(ProcessEvent)} cannot be called.");

            protected override IAsyncEnumerable<UICommandEvent> GetAny(CancellationToken cancellationToken)
                => AsyncEnumerable.Empty<UICommandEvent>();

            internal NoUICommandTracker() : base(MomentClockSet.Default) { }
        }

        private static readonly UnboundedChannelOptions ChannelOptions =
            new() { AllowSynchronousContinuations = false };

        public static IUICommandTracker None { get; } = new NoUICommandTracker();

        private readonly HashSet<Channel<UICommandEvent>> _channels = new();
        private Task<UICommandEvent> _whenAnyEventTask = null!;
        private Task<UICommandEvent> _whenCommandStartedTask = null!;
        private Task<UICommandEvent> _whenCommandCompletedTask = null!;
        private Task<UICommandEvent> _whenCommandFailedTask = null!;

        // ReSharper disable once InconsistentlySynchronizedField
        protected object Lock => _channels;
        protected bool IsDisposed { get; private set; }

        public MomentClockSet Clocks { get; }
        public IAsyncEnumerable<UICommandEvent> Events => GetAny();

        public UICommandEvent? LastEvent { get; protected set; }
        public UICommandEvent? LastCommandStarted { get; protected set; }
        public UICommandEvent? LastCommandCompleted { get; protected set; }
        public UICommandEvent? LastCommandFailed { get; protected set; }

        public Task<UICommandEvent> WhenAnyEvent() => _whenAnyEventTask;
        public Task<UICommandEvent> WhenCommandStarted() => _whenCommandStartedTask;
        public Task<UICommandEvent> WhenCommandCompleted() => _whenCommandCompletedTask;
        public Task<UICommandEvent> WhenCommandFailed() => _whenCommandFailedTask;

        public UICommandTracker(MomentClockSet clocks)
        {
            Clocks = clocks;
            RenewTasks();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Intentionally ignore disposing flag here
            lock (Lock) {
                if (IsDisposed)
                    return;
                IsDisposed = true;
                foreach (var channel in _channels)
                    channel.Writer.Complete();
            }
        }

        public virtual UICommandEvent ProcessEvent(UICommandEvent commandEvent)
        {
            if (!commandEvent.CreatedAt.HasValue)
                commandEvent = commandEvent with { CreatedAt = Clocks.UIClock.Now };
            if (commandEvent.IsCompleted && !commandEvent.CompletedAt.HasValue)
                commandEvent = commandEvent with { CompletedAt = Clocks.UIClock.Now };
            lock (Lock) {
                if (IsDisposed)
                    return commandEvent;
                LastEvent = commandEvent;
                TaskSource.For(_whenAnyEventTask).SetResult(commandEvent);
                if (commandEvent.IsCompleted) {
                    LastCommandCompleted = commandEvent;
                    TaskSource.For(_whenCommandCompletedTask).SetResult(commandEvent);
                    if (commandEvent.IsFailed) {
                        LastCommandFailed = LastEvent;
                        TaskSource.For(_whenCommandFailedTask).SetResult(commandEvent);
                    }
                }
                else {
                    LastCommandStarted = LastEvent;
                    TaskSource.For(_whenCommandStartedTask).SetResult(commandEvent);
                }
                foreach (var channel in _channels)
                    channel.Writer.TryWrite(commandEvent);
                RenewTasks();
            }
            return commandEvent;
        }

        // Protected methods

        protected virtual async IAsyncEnumerable<UICommandEvent> GetAny(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<UICommandEvent>(ChannelOptions);
            lock (Lock) {
                if (IsDisposed)
                    yield break;
                _channels.Add(channel);
            }
            var reader = channel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (reader.TryRead(out var item))
                yield return item;
            lock (Lock) {
                _channels.Remove(channel);
            }
        }

        protected void RenewTasks()
        {
            lock (Lock) {
                if (_whenAnyEventTask?.IsCompleted ?? true)
                    _whenAnyEventTask = TaskSource.New<UICommandEvent>(true).Task;
                if (_whenCommandStartedTask?.IsCompleted ?? true)
                    _whenCommandStartedTask = TaskSource.New<UICommandEvent>(true).Task;
                if (_whenCommandCompletedTask?.IsCompleted ?? true)
                    _whenCommandCompletedTask = TaskSource.New<UICommandEvent>(true).Task;
                if (_whenCommandFailedTask?.IsCompleted ?? true)
                    _whenCommandFailedTask = TaskSource.New<UICommandEvent>(true).Task;
            }
        }
    }
}
