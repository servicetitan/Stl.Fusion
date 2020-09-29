using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    public class MathService
    {
        [ComputeMethod]
        public virtual Task<int> SumAsync(int[]? values, CancellationToken cancellationToken = default)
            => Task.FromResult(values?.Sum() ?? 0);
    }
}
