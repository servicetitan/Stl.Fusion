using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Stl.Fusion.EntityFramework.Internal;

public sealed class DbHintFormatterExtensionInfo(IDbContextOptionsExtension extension)
    : DbContextOptionsExtensionInfo(extension)
{
    public new DbHintFormatterOptionsExtension Extension => (DbHintFormatterOptionsExtension) base.Extension;
    public override bool IsDatabaseProvider => false;

    public override string LogFragment {
        get {
            var extension = Extension;
            return $"DbHintFormatterType={extension.DbHintFormatterType}";
        }
    }

#if NET6_0_OR_GREATER
    public override int GetServiceProviderHashCode() => 0;

    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
    {
        if (other is not DbHintFormatterExtensionInfo otherInfo)
            return false;

        return otherInfo.Extension.DbHintFormatterType == Extension.DbHintFormatterType;
    }
#else
    public override long GetServiceProviderHashCode() => 0;
#endif

    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    { }
}
