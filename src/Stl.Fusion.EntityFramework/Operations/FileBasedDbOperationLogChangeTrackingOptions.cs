using Stl.Multitenancy;
using Stl.IO;

namespace Stl.Fusion.EntityFramework.Operations;

public record FileBasedDbOperationLogChangeTrackingOptions<TDbContext> : DbOperationCompletionTrackingOptions
{
    public static FileBasedDbOperationLogChangeTrackingOptions<TDbContext> Default { get; set; } = new();

    public Func<Tenant, FilePath> FilePathFactory { get; init; } = DefaultFilePathFactory;

    public static FilePath DefaultFilePathFactory(Tenant tenant)
    {
        var tDbContext = typeof(TDbContext);
        var appTempDir = FilePath.GetApplicationTempDirectory("", true);
        var tenantSuffix = tenant == Tenant.Default ? "" : $"_{tenant.Id.Value}";
        return appTempDir & FilePath.GetHashedName($"{tDbContext.Name}_{tDbContext.Namespace}{tenantSuffix}.tracker");
    }
}
