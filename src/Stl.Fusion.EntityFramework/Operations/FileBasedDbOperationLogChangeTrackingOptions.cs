using Stl.IO;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class FileBasedDbOperationLogChangeTrackingOptions<TDbContext>
    {
        public PathString FilePath { get; set; }

        public FileBasedDbOperationLogChangeTrackingOptions()
        {
            var tDbContext = typeof(TDbContext);
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            FilePath = appTempDir & PathEx.GetHashedName($"{tDbContext.Name}_{tDbContext.Namespace}.tracker");
        }
    }
}
