using System;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Bridge;

namespace Stl.Samples.Blazor.Services
{
    public interface ITimeProviderClient
    {
        [Get("")]
        Task<IComputedReplica<DateTime>> GetTimeAsync();
    }
}
