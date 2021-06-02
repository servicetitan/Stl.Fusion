using System;
using FluentAssertions;
using Stl.OS;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.OS
{
    public class MiscInfoTest : TestBase
    {
        public MiscInfoTest(ITestOutputHelper @out) : base(@out) { }

        void WriteLine(string line) => Out.WriteLine(line);

        [Fact]
        public void OSInfoTest()
        {
            WriteLine($"OS type:   {OSInfo.Kind}");
            WriteLine($"User home: {OSInfo.UserHomePath}");
        }

        [Fact]
        public void HardwareInfoTest()
        {
            var processorCount = HardwareInfo.GetProcessorCountFactor();
            processorCount.Should().BeGreaterThan(0);
            WriteLine($"CPU core count: {processorCount}");
        }

        #if !NETFRAMEWORK

        [Fact]
        public void DotNetCoreInfoTest()
        {
            var version = RuntimeInfo.DotNetCore.Version;
            version.Should().BeGreaterThan(Version.Parse("3.0"));
            WriteLine($".NET Core version: {version}");
        }

        #endif
    }
}
