using System;
using System.Threading.Tasks;

namespace Stl.Fusion.Extensions
{
    public interface IFusionTime
    {
        Task<DateTime> GetUtcNow();
        Task<DateTime> GetUtcNow(TimeSpan updatePeriod);
        Task<string> GetMomentsAgo(DateTime time);
    }
}
