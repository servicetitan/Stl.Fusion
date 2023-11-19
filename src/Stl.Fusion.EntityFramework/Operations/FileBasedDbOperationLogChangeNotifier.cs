using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public class FileBasedDbOperationLogChangeNotifier<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
    (FileBasedDbOperationLogChangeTrackingOptions<TDbContext> options, IServiceProvider services)
    : DbOperationCompletionNotifierBase<TDbContext, FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>(options, services)
    where TDbContext : DbContext
{
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
