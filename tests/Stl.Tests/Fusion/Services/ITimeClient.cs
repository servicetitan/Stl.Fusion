using System;
using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;

namespace Stl.Tests.Fusion.Services
{
    [Header(FusionHeaders.RequestPublication, "1")]
    public interface ITimeClient : IReplicaService
    {
        [Get("get")]
        Task<IComputed<DateTime>> GetComputedTimeAsync(CancellationToken cancellationToken = default);

        [Get("get")]
        Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default);
    }
}
