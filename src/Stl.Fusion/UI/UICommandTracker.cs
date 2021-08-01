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
        IMomentClock Clock { get; }

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
        public class Options
        {
            public IMomentClock Clock { get; set; } = CpuClock.Instance;
        }

        private class NoUICommandTracker : UICommandTracker
        {
            public override UICommandEvent ProcessEvent(UICommandEvent commandEvent)
                => throw Errors.InternalError($"{GetType().Name}.{nameof(ProcessEvent)} cannot be called.");

            protected override IAsyncEnumerable<UICommandEvent> GetAny(CancellationToken cancellationToken)
                => AsyncEnumerable.Empty<UICommandEvent>();

            internal NoUICommandTracker() : base(new()) { }
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

        public IMomentClock Clock { get; }
        public IAsyncEnumerable<UICommandEvent> Events => GetAny();

        public UICommandEvent? LastEvent { get; protected set; }
        public UICommandEvent? LastCommandStarted { get; protected set; }
        public UICommandEvent? LastCommandCompleted { get; protected set; }
        public UICommandEvent? LastCommandFailed { get; protected set; }

        public Task<UICommandEvent> WhenAnyEvent() => _whenAnyEventTask;
        public Task<UICommandEvent> WhenCommandStarted() => _whenCommandStartedTask;
        public Task<UICommandEvent> WhenCommandCompleted() => _whenCommandCompletedTask;
        public Task<UICommandEvent> WhenCommandFailed() => _whenCommandFailedTask;

        public UICommandTracker(Options? options)
        {
            options ??= new();
            Clock = options.Clock;
            RenewTasks();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (Lock) {
                if (IsDisposed)
                    return;
                foreach (var channel in _channels)
                    channel.Writer.Complete();
                var exception = Errors.AlreadyDisposed();
                if (_whenAnyEventTask != null!)
                    TaskSource.For(_whenAnyEventTask).TrySetException(exception);
                if (_whenCommandStartedTask != null!)
                    TaskSource.For(_whenCommandStartedTask).TrySetException(exception);
                if (_whenCommandCompletedTask != null!)
                    TaskSource.For(_whenCommandCompletedTask).TrySetException(exception);
                if (_whenCommandFailedTask != null!)
                    TaskSource.For(_whenCommandFailedTask).TrySetException(exception);
                IsDisposed = true;
            }
        }

        public virtual UICommandEvent ProcessEvent(UICommandEvent commandEvent)
        {
            if (!commandEvent.IsCompleted) {
                if (!commandEvent.CreatedAt.HasValue)
                    commandEvent = commandEvent with { CreatedAt = Clock.Now };
            }
            else {
                if (!commandEvent.CompletedAt.HasValue)
                    commandEvent = commandEvent with { CompletedAt = Clock.Now };
            }
            lock (Lock) {
                if (IsDisposed)
                    throw Errors.AlreadyDisposed();
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
                    throw Errors.AlreadyDisposed();
                _channels.Add(channel);
            }
            var reader = channel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var item))
                    continue;
                yield return item;
            }
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
