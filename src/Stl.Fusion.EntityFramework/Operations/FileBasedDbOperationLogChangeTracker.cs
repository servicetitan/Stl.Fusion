using Microsoft.EntityFrameworkCore;
using Stl.IO;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public class FileBasedDbOperationLogChangeTracker<TDbContext> 
    : DbOperationCompletionTrackerBase<TDbContext, FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>
    where TDbContext : DbContext
{
    public FileBasedDbOperationLogChangeTracker(
        FileBasedDbOperationLogChangeTrackingOptions<TDbContext> options, 
        IServiceProvider services) 
        : base(options, services) { }

    protected override DbOperationCompletionTrackerBase.TenantWatcher CreateTenantWatcher(Symbol tenantId) 
        => new TenantWatcher(this, tenantId);

    protected new class TenantWatcher : DbOperationCompletionTrackerBase.TenantWatcher
    {
        protected FileBasedDbOperationLogChangeTracker<TDbContext> Owner { get; }
        protected FilePath FilePath { get; }
        protected FileSystemWatcher Watcher { get; init; }
        protected IObservable<FileSystemEventArgs> Observable { get; init; }
        protected IDisposable Subscription { get; init; }

        public TenantWatcher(FileBasedDbOperationLogChangeTracker<TDbContext> owner, Symbol tenantId) 
            : base(owner.TenantRegistry.Get(tenantId))
        {
            Owner = owner;
            FilePath = Owner.Options.FilePathFactory.Invoke(Tenant);
            Watcher = new FileSystemWatcher(FilePath.DirectoryPath, FilePath.FileName);
            Observable = Watcher.ToObservable();
            Subscription = Observable.Subscribe(_ => CompleteWaitForChanges());
            Watcher.EnableRaisingEvents = true;
        }
    }
}
