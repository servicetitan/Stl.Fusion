using RestEase;
using Stl.Fusion.Client;

namespace Stl.Fusion.Tests.Services;

[RegisterRestEaseReplicaService(typeof(IClientTimeService), Scope = ServiceScope.ClientServices)]
[BasePath("time")]
public interface ITimeServiceClient
{
    [Get(nameof(GetTime))]
    Task<DateTime> GetTime(CancellationToken cancellationToken = default);
    [Get(nameof(GetTimeNoControllerMethod) + "_")]
    Task<DateTime> GetTimeNoControllerMethod(CancellationToken cancellationToken = default);
    [Get(nameof(GetTimeNoPublication))]
    Task<DateTime> GetTimeNoPublication(CancellationToken cancellationToken = default);
    [Get(nameof(GetFormattedTime))]
    Task<string?> GetFormattedTime(string format, CancellationToken cancellationToken = default);
}

public interface IClientTimeService : ITimeService { }
