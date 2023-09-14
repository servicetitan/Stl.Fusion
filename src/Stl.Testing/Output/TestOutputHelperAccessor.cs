using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace Stl.Testing.Output;

public class TestOutputHelperAccessor(ITestOutputHelper? output) : ITestOutputHelperAccessor
{
    public ITestOutputHelper? Output { get; set; } = output;
}
