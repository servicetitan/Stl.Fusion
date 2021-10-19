using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace Stl.Testing.Output;

public class TestOutputHelperAccessor : ITestOutputHelperAccessor
{
    public ITestOutputHelper? Output { get; set; }

    public TestOutputHelperAccessor(ITestOutputHelper? output)
        => Output = output;
}
