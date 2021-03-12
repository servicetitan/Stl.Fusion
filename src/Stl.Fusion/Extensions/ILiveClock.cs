using System;
using System.Threading.Tasks;

namespace Stl.Fusion.Extensions
{
    public interface ILiveClock
    {
        Task<DateTime> GetUtcNow();
        Task<DateTime> GetUtcNow(TimeSpan updatePeriod);
        Task<string> GetMomentsAgo(DateTime time);
    }
}
