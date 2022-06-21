using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations;

public record NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>
    where TDbContext : DbContext
{
    public string ChannelName { get; init; } = "_Operations";
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public int RetryCount { get; init; } = 10;
}
