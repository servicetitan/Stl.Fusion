using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion;
using static System.Console;

namespace Tutorial
{
    public static class Part04
    {
        #region part04_createHelper
        public static TService Create<TService>()
            where TService : class, IComputedService
        {
            var providerFactory = new DefaultServiceProviderFactory();
            var services = new ServiceCollection();
            services.AddFusionCore();
            services.AddComputedService<TService>();
            var provider = providerFactory.CreateServiceProvider(services);
            return provider.GetRequiredService<TService>();
        }
        #endregion

        public static Task DefineCalculator() => Task.CompletedTask;
        #region part04_defineCalculator
        public class Calculator : IComputedService
        {
            [ComputedServiceMethod]
            public virtual async Task<double> SumAsync(double a, double b)
            {
                WriteLine($"  + {nameof(SumAsync)}({a}, {b})");
                await Task.Delay(100);
                WriteLine($"  - {nameof(SumAsync)}({a}, {b})");
                return a + b;
            }
        }
        #endregion

        public static Task DefineTestCalculator() => Task.CompletedTask;
        #region part04_defineTestCalculator
        static async Task TestCalculator(Calculator calculator)
        {
            WriteLine($"Testing '{calculator.GetType()}':");
            var tasks = new List<Task<double>> {
                calculator.SumAsync(1, 1),
                calculator.SumAsync(1, 1),
                calculator.SumAsync(1, 1),
                calculator.SumAsync(1, 2),
                calculator.SumAsync(1, 2)
            };
            await Task.WhenAll(tasks);
            var sum = tasks.Sum(t => t.Result);
            WriteLine($"Sum of results: {sum}");
        }
        #endregion

        public static async Task UseCalculator1()
        {
            #region part04_useCalculator1
            var normalCalculator = new Calculator();
            await TestCalculator(normalCalculator);

            var fusionCalculator = Create<Calculator>();
            await TestCalculator(fusionCalculator);
            #endregion
        }

        public static async Task UseCalculator2()
        {
            #region part04_useCalculator2
            var c = Create<Calculator>();
            for (var i = 0; i < 10; i++) {
                await TestCalculator(c);
                await Task.Delay(1000);
            }
            #endregion
        }

        public static async Task UseCalculator3()
        {
            #region part04_useCalculator3
            var c = Create<Calculator>();
            await TestCalculator(c);
            
            // Default ComputedOptions.KeepAliveTime is 1s, we need to
            // wait at least this time to make sure the following Prune call
            // will evict the entry.
            await Task.Delay(2000); 
            var registry = ComputedRegistry.Default;
            var mPrune = registry.GetType().GetMethod("Prune", BindingFlags.Instance | BindingFlags.NonPublic);
            mPrune.Invoke(registry, Array.Empty<object>());
            GC.Collect();

            await TestCalculator(c);
            #endregion
        }

        public static async Task UseCalculator4()
        {
            #region part04_useCalculator4
            var c = Create<Calculator>();
            await TestCalculator(c);
            
            await Task.Delay(2000);
            for (var i = 0; i < 20_000; i++)
                c.SumAsync(5, 5);
            GC.Collect();

            await TestCalculator(c);
            #endregion
        }
    }
}
