using System;
using System.Collections.Generic;
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
            var services = new ServiceCollection()
                .AddFusionCore()
                .AddComputedService<TService>();

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<TService>();
        }
        #endregion

        public static Task DefineCalculator() => Task.CompletedTask;
        #region part04_defineCalculator
        public class Calculator : IComputedService
        {
            [ComputedServiceMethod]
            public virtual async Task<double> SumAsync(double a, double b, bool logEnterExit = true)
            {
                if (logEnterExit)
                    WriteLine($"+ {nameof(SumAsync)}({a}, {b})");
                await Task.Delay(100);
                if (logEnterExit)
                    WriteLine($"- {nameof(SumAsync)}({a}, {b})");
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
            await Task.Delay(1100);
            var registry = ComputedRegistry.Default;
            var mPrune = registry.GetType().GetMethod("Prune", BindingFlags.Instance | BindingFlags.NonPublic);
            mPrune!.Invoke(registry, Array.Empty<object>());
            GC.Collect();

            await TestCalculator(c);
            #endregion
        }

        public static async Task UseCalculator4()
        {
            #region part04_useCalculator4
            var c = Create<Calculator>();
            await TestCalculator(c);

            await Task.Delay(1100);
            var tasks = new List<Task>();
            for (var i = 0; i < 200_000; i++)
                tasks.Add(c.SumAsync(3, i, false));
            await Task.WhenAll(tasks);
            GC.Collect();

            await TestCalculator(c);
            #endregion
        }

        public static async Task UseCalculator5()
        {
            #region part04_useCalculator5
            var calc = Create<Calculator>();

            var s1 = await calc.SumAsync(1, 1);
            WriteLine($"{nameof(s1)} = {s1}");

            // Now let's pull the computed instance that represents the result of
            // Notice that the underlying SumAsync code won't be invoked,
            // since the result is already cached:
            var c1 = await Computed.CaptureAsync(_ => calc.SumAsync(1, 1));
            WriteLine($"{nameof(c1)} = {c1}, Value = {c1.Value}");

            // And invalidate it
            c1.Invalidate();
            WriteLine($"{nameof(c1)} = {c1}, Value = {c1.Value}");

            // Let's compute the sum once more now.
            // You'll see that SumAsync gets invoked this time.
            s1 = await calc.SumAsync(1, 1);
            WriteLine($"{nameof(s1)} = {s1}");
            #endregion
        }

        public static async Task UseCalculator6()
        {
            #region part04_useCalculator6
            var calc = Create<Calculator>();

            WriteLine("Calling & invalidating SumAsync(1, 1)");
            WriteLine(await calc.SumAsync(1, 1));
            // This will lead to re-computation
            Computed.Invalidate(() => calc.SumAsync(1, 1));
            WriteLine(await calc.SumAsync(1, 1));

            WriteLine("Calling SumAsync(2, 2), but invalidating SumAsync(2, 3)");
            WriteLine(await calc.SumAsync(2, 2));
            // But this won't - because the arguments are (2, 3), not (2, 2)
            Computed.Invalidate(() => calc.SumAsync(2, 3));
            WriteLine(await calc.SumAsync(2, 2));
            #endregion
        }
    }
}
