using System;
using System.Collections.Generic;

namespace Stl.Internal
{
    public static class DependencySorter
    {
        public static IEnumerable<T> OrderByDependency<T>(
            this IEnumerable<T> source, 
            Func<T, IEnumerable<T>> dependencySelector)
        {
            var processing = new HashSet<T>();
            var processed = new HashSet<T>();
            var stack = new Stack<T>(source);

            while (stack.TryPop(out var item)) {
                if (processing.Contains(item))
                    throw Errors.CircularDependency(item);
                if (processed.Contains(item))
                    continue;
                processing.Add(item);
                foreach (var dependency in dependencySelector(item))
                    stack.Push(dependency);
                processing.Remove(item);
                yield return item;
                processed.Add(item);
            }
        }
    }
}
