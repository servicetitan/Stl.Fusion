using Stl.IO;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class FileBasedDbOperationLogChangeTrackingOptions<TDbContext>
    {
        public PathString FilePath { get; set; }

        public FileBasedDbOperationLogChangeTrackingOptions()
        {
            var tDbContext = typeof(TDbContext);
            var appTempDir = PathExt.GetApplicationTempDirectory("", true);
            FilePath = appTempDir & PathExt.GetHashedName($"{tDbContext.Name}_{tDbContext.Namespace}.tracker");
        }
    }
}
