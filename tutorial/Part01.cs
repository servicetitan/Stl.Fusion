using System;
using System.Threading.Tasks;
using Stl;
using Stl.Fusion;
using static System.Console;

namespace Tutorial
{
    public static class Part01
    {
        public static async Task Create()
        {
            #region part01_create
            // Later we'll show you much nicer ways to create IComputed instances,
            // but for now let's stick to the basics:
            var cCount = SimpleComputed.New(async (prev, ct) => prev.Value + 1, Result.New(1));
            WriteLine(cCount.State);
            WriteLine(cCount.Value);
            #endregion
        }

        public static async Task Invalidate()
        {
            #region part01_invalidate
            var cCount = SimpleComputed.New(async (prev, ct) => prev.Value + 1, Result.New(1));
            cCount.Invalidate();
            WriteLine(cCount.State);
            WriteLine(cCount.Value);
            #endregion
        }

        public static async Task TheRest()
        {
            #region update
            var cCount = SimpleComputed.New(async (prev, ct) => prev.Value + 1, Result.New(1));
            var cCount1 = await cCount.UpdateAsync(false);
            WriteLine(cCount1.State);
            WriteLine(cCount1.Value);
            #endregion

            #region immutability
            // For the sake of clarity, equality isn't overriden for any implementation of IComputed, 
            // so it relies on default Equals & GetHashCode, i.e. it performs by-ref comparison.   
            WriteLine($"Is {nameof(cCount)} and {nameof(cCount1)} pointing to the same instance? {cCount == cCount1}");
            WriteLine($"{nameof(cCount)}: {cCount}, Value = {cCount.Value}");
            WriteLine($"{nameof(cCount1)}: {cCount1}, Value = {cCount1.Value}");
            #endregion

            #region dependency  
            // Let's get the most "up to date" cCount instance
            cCount = await cCount.UpdateAsync(false);

            // And create another computed that uses it:
            var cCountTitle = await SimpleComputed.New<string>(async (prev, ct) => {
                var count = await cCount.UseAsync(ct);
                return $"cCount = {count}";
                // Note that we update it right on the next line - we use a
                // SimpleComputed.New version w/o a default value here, and 
                // it implies our initial IComputed will be in invalidated state
                // (and have default(string) value), but since we'd like to
                // see a real computed value for it, we update it right after the
                // creation.
            }).UpdateAsync(false);
            WriteLine($"{nameof(cCountTitle)}: {cCountTitle}, Value = {cCountTitle.Value}");

            // And invalidate it
            cCount.Invalidate();
            // cCount should be invalidated:
            WriteLine($"{nameof(cCount)}: {cCount}, Value = {cCount.Value}");
            // Note that cCountTitle is invalidated too, because it is dependent on cCount 
            WriteLine($"{nameof(cCountTitle)}: {cCountTitle}, Value = {cCountTitle.Value}");
            #endregion

            #region depdencency_recompute
            // Now, let's update cCountTitle:
            cCountTitle = await cCountTitle.UpdateAsync(false);
            // As you see, the update also triggered recompute of cCount (its dependency)!
            WriteLine($"{nameof(cCountTitle)}: {cCountTitle}, Value = {cCountTitle.Value}");
            #endregion

            #region dependency_complex_1
            // Let's get the most "up to date" cCount instance
            cCount = await cCount.UpdateAsync(false);

            // And declare a new computed
            var cTime = SimpleComputed.New<DateTime>(async (prev, ct) => DateTime.Now);
            // It expected to be invalidated & have default(DateTime) value - that's the
            // default behavior for SimpleComputed.New(...) w/o initial value.
            WriteLine($"{nameof(cTime)}: {cTime}, Value = {cTime.Value}");
            #endregion

            #region dependency_complex_2
            // Now, let's create a dependency that depends on everything:
            var cSummary = await SimpleComputed.New<string>(async (prev, ct) => {
                var countTitle = cCountTitle.UseAsync(ct);
                var time = await cTime.UseAsync(ct);
                return $"{time}: {countTitle}";
            }).UpdateAsync(false);
            WriteLine($"{nameof(cSummary)}: {cSummary}, Value = {cSummary.Value}");
            #endregion

            #region dependency_complex_3
            // Let's invalidate cCount:
            cCount.Invalidate();
            WriteLine($"{nameof(cCount)}:  {cCount}, Value = {cCount.Value}");

            // As you might guess, this also invalidated:
            WriteLine($"{nameof(cSummary)}: {cSummary}, Value = {cSummary.Value}");
            WriteLine($"{nameof(cCountTitle)}: {cCountTitle}, Value = {cCountTitle.Value}");
            #endregion

            #region dependency_complex_4
            // And finally, let's update the summary:
            cSummary = await cSummary.UpdateAsync(false);
            // Notice that cCount was updated, even though cSummary doesn't depend on it directly?
            WriteLine($"{nameof(cSummary)}: {cSummary}, Value = {cSummary.Value}");
            #endregion
        }
    }
}
