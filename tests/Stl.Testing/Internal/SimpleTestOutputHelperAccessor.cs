using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace Stl.Testing.Internal
{
    public class SimpleTestOutputHelperAccessor : ITestOutputHelperAccessor
    {
        public ITestOutputHelper Output { get; set; }

        public SimpleTestOutputHelperAccessor(ITestOutputHelper output) => Output = output;
    }
}
