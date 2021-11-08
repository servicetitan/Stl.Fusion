namespace Stl.Fusion.Extensions;

public interface IFusionTime
{
    [ComputeMethod]
    Task<DateTime> GetUtcNow();
    [ComputeMethod]
    Task<DateTime> GetUtcNow(TimeSpan updatePeriod);
    [ComputeMethod]
    Task<string> GetMomentsAgo(DateTime time);
}
