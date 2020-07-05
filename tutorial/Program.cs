using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections;
using Stl.Reflection;

namespace Tutorial
{
    public class Program
    {
        public static async Task Main(
            string? region = null,
            string? session = null,
            string? package = null,
            string? project = null,
            string[]? args = null)
        {
            region ??= "";
            var parts = region.Split("_", 2).Select(s => s.ToLowerInvariant()).ToArray();
            var typeName = parts[0];
            var methodName = parts[1];
            
            var type = typeof(Program).Assembly.GetTypes()
                .SingleOrDefault(t => t.Name.ToLowerInvariant() == typeName && !@t.IsAbstract);
            if (type == null)
                throw new ArgumentOutOfRangeException(nameof(region));
            var method = type.GetMethod(methodName);
            if (method == null)
                throw new ArgumentOutOfRangeException(nameof(region));

            var instance = type.CreateInstance();
            var result = method.Invoke(instance, Array.Empty<object>());
            if (result is Task task)
                await task;
        }
    }
}
