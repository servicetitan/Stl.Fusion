using System;
using System.Threading.Tasks;

namespace Templates.Blazor2.Abstractions
{
    public interface IMomentsAgoService
    {
        Task<string> GetMomentsAgoAsync(DateTime time);
    }
}
