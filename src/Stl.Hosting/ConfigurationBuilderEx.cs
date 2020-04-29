using System;
using Microsoft.Extensions.Configuration;
using Stl.Hosting.Internal;

namespace Stl.Hosting
{
    public static class ConfigurationBuilderEx
    {
        public static IConfigurationBuilder AddFile(this IConfigurationBuilder builder, 
            string fileName, bool optional = true, bool reloadOnChange = false)
        {
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                builder.AddJsonFile(fileName, optional, reloadOnChange);
            else if (fileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                builder.AddIniFile(fileName, optional, reloadOnChange);
            else if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                builder.AddXmlFile(fileName, optional, reloadOnChange);
            else 
                throw Errors.UnknownConfigurationFileType(fileName);
            return builder;
        }
    }
}
