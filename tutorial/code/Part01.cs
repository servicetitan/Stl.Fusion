using System;
using System.Threading;
using System.Threading.Tasks;
using Stl;
using Stl.Fusion;

namespace Tutorial
{
    public class Part01 : ITutorialPart
    {
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            #region create
            // Later we'll show you much nicer ways to create IComputed instances,
            // but for now let's stick to the basics:
            var age = SimpleComputed.New(async (prev, ct) => prev.Value + 1, Result.New(1));
            Console.WriteLine(age.State);
            Console.WriteLine(age.Value);
            #endregion

            #region invalidate
            age.Invalidate();
            Console.WriteLine(age.State);
            Console.WriteLine(age.Value);
            #endregion

            #region update
            var age1 = await age.UpdateAsync(false, cancellationToken);
            Console.WriteLine(age1.State);
            Console.WriteLine(age1.Value);
            #endregion

            #region immutability
            // For the sake of clarity, equality isn't overriden for any implementation of IComputed, 
            // so it relies on default Equals & GetHashCode, i.e. it performs by-ref comparison.   
            Console.WriteLine($"Is age and age1 pointing to the same instance? {age == age1}");
            Console.WriteLine($"age:  {age}, Value = {age.Value}");
            Console.WriteLine($"age1: {age1}, Value = {age1.Value}");
            #endregion

            #region dependency  
            var ageString = await SimpleComputed.New<string>(async (prev, ct) => {
                var ageValue = await age.UseAsync(ct);
                return $"Look, I am {ageValue} years old!";
            }).UpdateAsync(false, cancellationToken);
            Console.WriteLine($"ageString: {ageString}, Value = {ageString.Value}");

            // Let's get the most "up to date" age now
            age = await age.UpdateAsync(false, cancellationToken);
            // It should be the same instance as age1 now
            Console.WriteLine(ReferenceEquals(age1, age));
            // And invalidate it
            age.Invalidate();
            // 'age' is expected to invalidate:
            Console.WriteLine($"age:  {age}, Value = {age.Value}");
            // But 'ageString' will be invalidated too, because it is dependent on 'age': 
            Console.WriteLine($"ageString: {ageString}, Value = {ageString.Value}");
            #endregion

            #region depdencency_recompute
            // Now, let's update 'ageString':
            ageString = await ageString.UpdateAsync(false);
            // As you see, its update triggered recomputation of its dependency too!
            Console.WriteLine($"ageString: {ageString}, Value = {ageString.Value}");
            #endregion
        }
    }
}
