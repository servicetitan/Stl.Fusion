using Stl.Fusion.Interception;

namespace Stl.Fusion.Extensions;

public interface IFusionTime : IComputeService
{
    [ComputeMethod]
    Task<DateTime> GetUtcNow();
    [ComputeMethod]
    Task<DateTime> GetUtcNow(TimeSpan updatePeriod);
    [ComputeMethod]
    Task<string> GetMomentsAgo(DateTime time);
}
