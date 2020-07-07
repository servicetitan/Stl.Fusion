using Xunit;

namespace Stl.Tests
{
    [CollectionDefinition(nameof(PerformanceTests), DisableParallelization = true)]
    public class PerformanceTests 
    { }

    [CollectionDefinition(nameof(TimeSensitiveTests), DisableParallelization = true)]
    public class TimeSensitiveTests 
    { }
}
