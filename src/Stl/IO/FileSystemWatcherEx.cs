using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.IO
{
    public static class FileSystemWatcherEx
    {
        public static IObservable<FileSystemEventArgs> ToObservable(this FileSystemWatcher watcher)
        {
            var o1 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    handler => {
                        watcher.Changed += handler;
                        watcher.Created += handler;
                        watcher.Deleted += handler;
                    },
                    handler => {
                        watcher.Changed -= handler;
                        watcher.Created -= handler;
                        watcher.Deleted -= handler;
                    })
                .Select(p => p.EventArgs);
            var o2 = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                    handler => {
                        watcher.Renamed += handler;
                    },
                    handler => {
                        watcher.Renamed -= handler;
                    })
                .Select(p => p.EventArgs);
            return o1.Merge(o2).Publish().RefCount();
        }

        public static Task<FileSystemEventArgs> GetFirstEvent(
            this FileSystemWatcher watcher,
            CancellationToken cancellationToken = default)
            => GetFirstEvent(watcher, TaskCreationOptions.None, cancellationToken);

        public static async Task<FileSystemEventArgs> GetFirstEvent(
            this FileSystemWatcher watcher,
            TaskCreationOptions taskCreationOptions,
            CancellationToken cancellationToken = default)
        {
            var ts = TaskSource.New<FileSystemEventArgs>(taskCreationOptions);
            var handler = (FileSystemEventHandler) ((_, args) => ts.TrySetResult(args));
            try {
                watcher.Changed += handler;
                watcher.Created += handler;
                watcher.Deleted += handler;
                return await ts.Task.WithFakeCancellation(cancellationToken).ConfigureAwait(false);
            }
            finally {
                watcher.Changed -= handler;
                watcher.Created -= handler;
                watcher.Deleted -= handler;
            }
        }
    }
}
