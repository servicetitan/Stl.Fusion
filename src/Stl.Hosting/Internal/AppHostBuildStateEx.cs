using System.Linq;
using Stl.Collections;

namespace Stl.Hosting.Internal
{
    public static class AppHostBuildStateEx
    {
        public static string[] ParsableArguments(this IAppHostBuildState state)
            => state.Arguments.AsEnumerable().TakeWhile(a => a != "--").ToArray();
        
        public static string[] UnparsableArguments(this IAppHostBuildState state)
            => state.Arguments.AsEnumerable().Skip(state.ParsableArguments().Length + 1).ToArray();
    }
}
