using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.DependencyInjection;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.CommandR.Services;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.CommandR
{
    public class CommandDispatcherTest : TestBase
    {
        public CommandDispatcherTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task LogCommandTest()
        {
            var services = CreateServices();
            var command = new LogCommand() { Message = "Hi!" };
            await services.CommandDispatcher().RunAsync(command);
        }

        [Fact]
        public async Task DivCommandTest()
        {
            var services = CreateServices();

            var command = new DivCommand() { Divisible = 4, Divisor = 2 };
            var result = await services.CommandDispatcher().RunAsync(command);
            result.Should().Be(2);
        }

        [Fact]
        public async Task DivByZeroCommandTest()
        {
            var services = CreateServices();

            var command = new DivCommand() { Divisible = 4, Divisor = 0 };
            await Assert.ThrowsAsync<DivideByZeroException>(async () => {
                await services.CommandDispatcher().RunAsync(command);
            });
        }

        [Fact]
        public async Task RecSumCommandTest()
        {
            var services = CreateServices();

            var command = new RecSumCommand() { Arguments = new double[] {1, 2, 3} };
            var result = await services.CommandDispatcher().RunAsync(command);
            result.Should().Be(6);
        }

        private IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => {
                var debugCategories = new List<string> {
                    "Stl.CommandR",
                    "Stl.Tests.CommandR",
                };

                bool LogFilter(string category, LogLevel level)
                    => debugCategories.Any(category.StartsWith) && level >= LogLevel.Debug;

                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
                // XUnit logging requires weird setup b/c otherwise it filters out
                // everything below LogLevel.Information
                logging.AddProvider(new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor(Out),
                    LogFilter));
            });

            services.AttributeScanner(nameof(CommandRTestModule)).AddServicesFrom(Assembly.GetExecutingAssembly());
            return services.BuildServiceProvider();
        }
    }
}
