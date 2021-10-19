namespace Stl.Fusion.EntityFramework.Npgsql.Operations;

public class NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>
{
    public string ChannelName { get; set; } = "_Operations";
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}
