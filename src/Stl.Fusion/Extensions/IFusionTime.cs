namespace Stl.Fusion.Extensions;

public interface IFusionTime : IComputeService
{
    [ComputeMethod]
    Task<Moment> Now();
    [ComputeMethod]
    Task<Moment> Now(TimeSpan updatePeriod);
    [ComputeMethod]
    Task<string> GetMomentsAgo(Moment moment);
}
