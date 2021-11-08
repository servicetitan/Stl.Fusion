using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Npgsql.Internal;

namespace Stl.Fusion.EntityFramework.Npgsql;

public static class DbContextOptionsBuilderExt
{
    public static DbContextOptionsBuilder UseNpgsqlHintFormatter(this DbContextOptionsBuilder dbContext)
        => dbContext.UseHintFormatter<NpgsqlDbHintFormatter>();
}
