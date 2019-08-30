using System;

namespace Stl.Hosting.Internal
{
    public static class Errors
    {
        public static Exception UnknownConfigurationFileType(string fileName)
            => new NotSupportedException($"Unknown type of configuration file: '{fileName}'."); 
    }
}
