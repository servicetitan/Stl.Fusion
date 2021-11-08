namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeTrackingOptions<TDbContext>
{
    public string PubSubKey { get; set; } = $"{typeof(TDbContext).Name}._Operations";
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}
