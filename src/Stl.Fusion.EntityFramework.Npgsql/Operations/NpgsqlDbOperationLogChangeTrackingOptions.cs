using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations;

public record NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> : DbOperationCompletionTrackingOptions
    where TDbContext : DbContext
{
    public static NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> Default { get; set; } = new();

    public string ChannelName { get; init; } = "_Operations";
}
