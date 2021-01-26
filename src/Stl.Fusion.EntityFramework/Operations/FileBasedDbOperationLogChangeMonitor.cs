using System;
using System.IO;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.Async;
using Stl.IO;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class FileBasedDbOperationLogChangeMonitor<TDbContext> : IDbOperationLogChangeMonitor<TDbContext>, IDisposable
        where TDbContext : DbContext
    {
        public FileSystemWatcher Watcher { get; }
        public bool OwnsWatcher { get; }
        protected IObservable<FileSystemEventArgs> Observable { get; }
        protected IDisposable Subscription { get; }
        protected Task<Unit> NextEventTask { get; set; } = null!;
        protected object Lock { get; } = new();

        public FileBasedDbOperationLogChangeMonitor(FileSystemWatcher watcher, bool ownsWatcher = true)
        {
            Watcher = watcher;
            OwnsWatcher = ownsWatcher;
            // ReSharper disable once VirtualMemberCallInConstructor
            ReplaceNextEventTask();
            Observable = Watcher.ToObservable();
            Subscription = Observable.Subscribe(OnWatcherEvent);
            Watcher.EnableRaisingEvents = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            Subscription.Dispose();
            if (OwnsWatcher)
                Watcher.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task WaitForChangesAsync(CancellationToken cancellationToken = default)
        {
            lock (Lock) {
                var task = NextEventTask;
                if (NextEventTask.IsCompleted)
                    ReplaceNextEventTask();
                return task;
            }
        }

        // Protected methods

        protected virtual void OnWatcherEvent(FileSystemEventArgs eventArgs)
        {
            lock (Lock)
                TaskSource.For(NextEventTask).TrySetResult(default);
        }

        protected virtual void ReplaceNextEventTask()
            => NextEventTask = TaskSource.New<Unit>(false).Task;
    }
}
