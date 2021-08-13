using System;
using Stl.Fusion.EntityFramework.Reprocessing;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Reprocessing
{
    public class CommandReprocessingStateTest : TestBase
    {
        public CommandReprocessingStateTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void ToStringTest()
        {
            var crs = new CommandReprocessingState() {
                FailureCount = 1,
                Error = new ApplicationException("Hey!", new NullReferenceException()),
            };
            var crsToString = crs.ToString();
            Out.WriteLine(crsToString);
        }
    }
}
