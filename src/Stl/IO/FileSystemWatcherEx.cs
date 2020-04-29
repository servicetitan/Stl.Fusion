using System;
using System.IO;
using System.Reactive.Linq;

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
    }
}
