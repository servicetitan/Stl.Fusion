using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Samples.Blazor.Common.Services
{
    public interface ITimeService
    {
        Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default);
    }
}
