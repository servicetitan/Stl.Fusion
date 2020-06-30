using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;

namespace Stl.Fusion.UI
{
    public interface IUpdateDelayer
    {
        Task DelayAsync(CancellationToken cancellationToken = default);
        Task ExtraErrorDelayAsync(Exception error, int tryIndex, CancellationToken cancellationToken = default);
        void CancelDelays(bool noDelay = false);
    }

    public class UpdateDelayer : IUpdateDelayer
    {
        public class Options
        {
            public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan MinExtraErrorDelay { get; set; } =  TimeSpan.FromSeconds(5);
            public TimeSpan MaxExtraErrorDelay { get; set; } = TimeSpan.FromMinutes(2);
            public TimeSpan CancelDelaysDelay { get; set; } = TimeSpan.FromSeconds(0.05);
            public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        }

        public static readonly IUpdateDelayer Default = new UpdateDelayer(new Options());
        private readonly ILogger _log;
        protected Task<Unit> EndDelayTask = null!;
        protected Task<Unit> ErrorEndDelayTask = null!;

        public TimeSpan Delay { get; }
        public TimeSpan MinExtraErrorDelay { get; }
        public TimeSpan MaxExtraErrorDelay { get; }
        public TimeSpan CancelDelaysDelay { get; } // Really sorry for this name :)
        public LogLevel LogLevel { get; }

        public UpdateDelayer(
            Options options,
            ILogger<UpdateDelayer>? log = null)
        {
            _log = log ??= NullLogger<UpdateDelayer>.Instance;

            Delay = options.Delay;
            MinExtraErrorDelay = options.MinExtraErrorDelay;
            MaxExtraErrorDelay = options.MaxExtraErrorDelay;
            CancelDelaysDelay = options.CancelDelaysDelay;
            LogLevel = options.LogLevel;
            EndDelay(ref EndDelayTask);
            EndDelay(ref ErrorEndDelayTask);
        }

        public virtual async Task DelayAsync(CancellationToken cancellationToken = default)
        {
            _log.Log(LogLevel, $"Delay started ({Delay.TotalSeconds:f2}s).");
            try {
                var mainDelayTask = Task.Delay(Delay, cancellationToken);
                var endDelayTask = Volatile.Read(ref EndDelayTask);
                await Task.WhenAny(endDelayTask, mainDelayTask).ConfigureAwait(false);
                _log.Log(LogLevel, $"Delay ended {(mainDelayTask.IsCompleted ? "normally": "via EndActiveDelays")}.");
            }
            catch (OperationCanceledException) {
                _log.Log(LogLevel, "Delay cancelled.");
            }
        }

        public virtual async Task ExtraErrorDelayAsync(Exception error, int tryIndex, CancellationToken cancellationToken = default)
        {
            var duration = Math.Pow(Math.Sqrt(2), tryIndex) * MinExtraErrorDelay.TotalSeconds;
            duration = Math.Min(MaxExtraErrorDelay.TotalSeconds, duration);
            _log.Log(LogLevel, $"Error delay started ({duration:f2}s).");
            try {
                var mainDelayTask = Task.Delay(TimeSpan.FromSeconds(duration), cancellationToken);
                var errorEndDelayTask = Volatile.Read(ref ErrorEndDelayTask);
                await Task.WhenAny(errorEndDelayTask, mainDelayTask).ConfigureAwait(false);
                _log.Log(LogLevel, $"Error delay ended {(mainDelayTask.IsCompleted ? "normally": "via EndActiveDelays")}.");
            }
            catch (OperationCanceledException) {
                _log.Log(LogLevel, "Error delay cancelled.");
            }
        }

        public virtual void CancelDelays(bool noDelay = false)
        {
            if (noDelay) {
                EndDelay(ref EndDelayTask);
                // We intentionally delay the cancellation here for 1s,
                // since otherwise it could enable rapid retries on errors. 
                EndDelay(ref ErrorEndDelayTask, TimeSpan.FromSeconds(1));
                return;
            }

            Task.Delay(CancelDelaysDelay, CancellationToken.None)
                .ContinueWith(_ => CancelDelays(true));
        }

        protected static void EndDelay(ref Task<Unit> task, TimeSpan withDelay = default)
        {
            var newTask = TaskSource.New<Unit>(true).Task;
            var oldTask = Interlocked.Exchange(ref task, newTask);
            if (oldTask == null)
                return;
            if (withDelay == default)
                TaskSource.For(oldTask).SetResult(default);
            else {
                var oldTaskCopy = oldTask;
                Task.Delay(withDelay, CancellationToken.None)
                    .ContinueWith(_ => TaskSource.For(oldTaskCopy).SetResult(default));
            }
        }
    }
}
