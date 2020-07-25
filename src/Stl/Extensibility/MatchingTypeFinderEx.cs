using System;
using System.Collections.Generic;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Extensibility
{
    public static class MatchingTypeFinderEx
    {
        public static Type? TryFind(this IMatchingTypeFinder matchingTypeFinder, Type source, Type scope)
            => matchingTypeFinder.TryFind(source, scope.ToSymbol());

        public static Type Find(this IMatchingTypeFinder matchingTypeFinder, Type scope, Symbol source)
            => matchingTypeFinder.TryFind(scope, source) ?? throw new KeyNotFoundException();
        public static Type Find(this IMatchingTypeFinder matchingTypeFinder, Type scope, Type source)
            => matchingTypeFinder.TryFind(scope, source) ?? throw new KeyNotFoundException();
    }
}
