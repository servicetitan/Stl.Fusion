using System;
using System.Threading.Tasks;

namespace Stl.Samples.Blazor.Common.Services
{
    public interface ITimeProvider
    {
        Task<DateTime> GetTimeAsync();
    }
}
