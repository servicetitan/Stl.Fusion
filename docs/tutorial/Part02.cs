using System;
using System.Threading.Tasks;
using Stl;
using Stl.Fusion;
using static System.Console;

namespace Tutorial
{
    public static class Part02
    {
        public static async Task Dependencies()
        {
            #region part02_dependencies
            WriteLine("Creating computed instances...");
            var cDate = SimpleComputed.New<DateTime>(async (prev, ct) => {
                var result = DateTime.Now;
                WriteLine($"Computing cDate: {result}");
                return result;
            });
            var cCount = SimpleComputed.New<int>(async (prev, ct) => {
                var result = prev.Value + 1;
                WriteLine($"Computing cCount: {result}");
                return result;
            });
            var cTitle = SimpleComputed.New<string>(async (prev, ct) => {
                var date = await cDate.UseAsync(ct);
                var count = await cCount.UseAsync(ct);
                var result = $"{date}: {count}";
                WriteLine($"Computing cTitle: {result}");
                return result;
            });

            WriteLine("All the computed values below should be in invalidated state.");
            WriteLine($"{cDate}, Value = {cDate.Value}"); 
            WriteLine($"{cCount}, Value = {cCount.Value}");
            WriteLine($"{cTitle}, Value = {cTitle.Value}");

            WriteLine();
            WriteLine("Let's trigger the computations:");
            cTitle = await cTitle.UpdateAsync(false);
            WriteLine($"{cDate}, Value = {cDate.Value}"); 
            WriteLine($"{cCount}, Value = {cCount.Value}");
            WriteLine($"{cTitle}, Value = {cTitle.Value}");

            WriteLine();
            WriteLine($"The next line won't trigger the computation, even though {nameof(cCount)} will be updated:");
            cCount = await cCount.UpdateAsync(false);
            WriteLine($"Let's do the same for {nameof(cDate)} now:");
            cDate = await cDate.UpdateAsync(false);

            WriteLine();
            WriteLine($"Let's invalidate {nameof(cCount)} and see what happens:");
            cCount.Invalidate();
            WriteLine($"{cCount}, Value = {cCount.Value}");
            WriteLine($"As you see, no computation is triggered so far.");
            WriteLine($"But notice that {nameof(cTitle)} is invalidated as well, because it depends on {nameof(cCount)}:");
            WriteLine($"{cTitle}, Value = {cTitle.Value}");

            WriteLine($"Finally, let's update {nameof(cTitle)} again:");
            cTitle = await cTitle.UpdateAsync(false);
            WriteLine($"{cTitle}, Value = {cTitle.Value}");
            #endregion
        }
    }
}
