using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public class FileBasedDbOperationLogChangeNotifier<TDbContext> 
    : DbOperationCompletionNotifierBase<TDbContext, FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>
    where TDbContext : DbContext
{
    public FileBasedDbOperationLogChangeNotifier(
        FileBasedDbOperationLogChangeTrackingOptions<TDbContext>? options, 
        IServiceProvider services) 
        : base(options, services) { }

    protected override Task Notify(Tenant tenant)
    {
        var filePath = Options.FilePathFactory(tenant);
        if (!File.Exists(filePath))
            File.WriteAllText(filePath, "");
        else
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
