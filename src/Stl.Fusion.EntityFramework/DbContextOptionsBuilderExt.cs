using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework;

public static class DbContextOptionsBuilderExt
{
    public static DbContextOptionsBuilder UseHintFormatter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbHintFormatter>(
        this DbContextOptionsBuilder dbContext)
        where TDbHintFormatter : IDbHintFormatter
    {
        var extension = new DbHintFormatterOptionsExtension(typeof(TDbHintFormatter));
        ((IDbContextOptionsBuilderInfrastructure) dbContext).AddOrUpdateExtension(extension);
        return dbContext;
    }
}
