using System;
using System.Threading.Tasks;

namespace Stl.Fusion.Extensions
{
    public interface ILiveClock
    {
        Task<DateTime> GetUtcNowAsync();
        Task<DateTime> GetUtcNowAsync(TimeSpan updatePeriod);
        Task<string> GetMomentsAgoAsync(DateTime time);
    }
}
