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
    public class FileBasedDbOperationLogChangeTracker<TDbContext> : IDbOperationLogChangeTracker<TDbContext>, IDisposable
        where TDbContext : DbContext
    {
        protected FileBasedDbOperationLogChangeTrackingOptions<TDbContext> Options { get; }
        protected FileSystemWatcher Watcher { get; }
        protected IObservable<FileSystemEventArgs> Observable { get; }
        protected IDisposable Subscription { get; }
        protected Task<Unit> NextEventTask { get; set; } = null!;
        protected object Lock { get; } = new();

        public FileBasedDbOperationLogChangeTracker(
            FileBasedDbOperationLogChangeTrackingOptions<TDbContext> options)
        {
            Options = options;
            Watcher = new FileSystemWatcher(options.FilePath.DirectoryPath, options.FilePath.FileName);
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
            Watcher.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task WaitForChanges(CancellationToken cancellationToken = default)
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
