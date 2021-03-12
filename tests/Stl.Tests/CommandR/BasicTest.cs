using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.CommandR;
using Stl.Testing;
using Stl.Tests.CommandR.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandR
{
    public class BasicTest : CommandRTestBase
    {
        public BasicTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task LogCommandTest()
        {
            var services = CreateServices();
            var command = new LogCommand() { Message = "Hi!" };
            await services.Commander().Call(command);
        }

        [Fact]
        public async Task DivCommandTest()
        {
            var services = CreateServices();

            var command = new DivCommand() { Divisible = 4, Divisor = 2 };
            var result = await services.Commander().Call(command);
            result.Should().Be(2);
        }

        [Fact]
        public async Task DivByZeroCommandTest()
        {
            var services = CreateServices();

            var command = new DivCommand() { Divisible = 4, Divisor = 0 };
            await Assert.ThrowsAsync<DivideByZeroException>(async () => {
                await services.Commander().Call(command);
            });
        }

        [Fact]
        public async Task RecSumCommandTest()
        {
            var services = CreateServices();
            RecSumCommand.Tag.Value = new();

            var result = await services.Commander().Call(new RecSumCommand() {
                Arguments = new double[] {1, 2, 3}
            });
            result.Should().Be(6);

            result = await services.Commander().Call(new RecSumCommand() {
                Arguments = new double[] {1, 2, 3, 4},
                Isolate = true,
            }, true);
            result.Should().Be(10);

        }
    }
}
