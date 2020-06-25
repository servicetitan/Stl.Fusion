using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Client.RestEase.Internal
{
    public static class Errors
    {
        public static Exception ComputedOfTExpected(string argumentName) 
            => new ArgumentOutOfRangeException(argumentName, 
                "Only typeof(IComputed<T>) values are supported for this argument.");
    }
}
