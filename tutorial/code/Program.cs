using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections;
using Stl.Reflection;

namespace Tutorial
{
    public interface ITutorialPart
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var parts = new HashSet<string>(args.Select(p => p.ToLowerInvariant()));
            var iTutorialPart = typeof(ITutorialPart);
            var types = (
                from t in typeof(Program).Assembly.GetTypes()
                where iTutorialPart.IsAssignableFrom(t) && !t.IsAbstract
                let name = t.Name.ToLowerInvariant()
                where parts.Count == 0 || parts.Contains(name)
                orderby name
                select t)
                .ToList();
            if (!types.Any())
                throw new ApplicationException(
                    $"Couldn't find any of the following parts: {parts.ToDelimitedString()}.");
            foreach (var type in types) {
                Console.WriteLine($"Running: {type.Name}");
                var instance = (ITutorialPart) type.CreateInstance();
                await instance.RunAsync();
            }
        }
    }
}
