using System;
using System.Collections.Generic;

namespace Stl.Extensibility
{
    public static class MatchingTypeFinderEx
    {
        public static Type Find(this IMatchingTypeFinder matchingTypeFinder, Type scope, Type source) 
            => matchingTypeFinder.TryFind(scope, source) ?? throw new KeyNotFoundException();
    }
}
