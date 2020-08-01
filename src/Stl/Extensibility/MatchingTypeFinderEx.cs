using System;
using System.Collections.Generic;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Extensibility
{
    public static class MatchingTypeFinderEx
    {
        public static Type? TryFind(this IMatchingTypeFinder matchingTypeFinder, Type source)
            => matchingTypeFinder.TryFind(source, Symbol.Empty);
        public static Type? TryFind(this IMatchingTypeFinder matchingTypeFinder, Type source, Type? scope)
            => matchingTypeFinder.TryFind(source, scope?.ToSymbol() ?? "");

        public static Type Find(this IMatchingTypeFinder matchingTypeFinder, Type source)
            => matchingTypeFinder.TryFind(source, Symbol.Empty) ?? throw new KeyNotFoundException();
        public static Type Find(this IMatchingTypeFinder matchingTypeFinder, Type source, Symbol scope)
            => matchingTypeFinder.TryFind(source, scope) ?? throw new KeyNotFoundException();
        public static Type Find(this IMatchingTypeFinder matchingTypeFinder, Type source, Type? scope)
            => matchingTypeFinder.TryFind(source, scope) ?? throw new KeyNotFoundException();
    }
}
