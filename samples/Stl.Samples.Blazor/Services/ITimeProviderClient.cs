using System;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;

namespace Stl.Samples.Blazor.Services
{
    [Header(FusionHeaders.Publish, "1")]
    public interface ITimeProviderClient
    {
        [Get("get")]
        Task<IComputedReplica<DateTime>> GetTimeAsync();
    }
}
