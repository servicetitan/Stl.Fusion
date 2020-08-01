using System;
using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Tests.Services
{
    [RestEaseReplicaService(typeof(IClientTimeService))]
    [RestEaseReplicaService(typeof(ITimeServiceClient))]
    [BasePath("time")]
    public interface ITimeServiceClient : IRestEaseReplicaClient
    {
        [Get("get")]
        Task<IComputed<DateTime>> GetComputedTimeAsync(CancellationToken cancellationToken = default);

        [Get("get")]
        Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default);
    }

    public interface IClientTimeService : ITimeService { }
}
