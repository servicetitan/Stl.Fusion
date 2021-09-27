using Stl.IO;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class FileBasedDbOperationLogChangeTrackingOptions<TDbContext>
    {
        public FilePath FilePath { get; set; }

        public FileBasedDbOperationLogChangeTrackingOptions()
        {
            var tDbContext = typeof(TDbContext);
            var appTempDir = FilePath.GetApplicationTempDirectory("", true);
            FilePath = appTempDir & FilePath.GetHashedName($"{tDbContext.Name}_{tDbContext.Namespace}.tracker");
        }
    }
}
