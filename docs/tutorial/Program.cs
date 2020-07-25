using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Tutorial
{
    public class Program
    {
        public static void Main(
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
                .SingleOrDefault(t => t.Name.ToLowerInvariant() == typeName);
            if (type == null)
                throw new ArgumentOutOfRangeException(nameof(region), $"No type named as '{typeName}' found.");
            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (method == null)
                throw new ArgumentOutOfRangeException(nameof(region), $"No method named as '{methodName}' found.");

            var result = method.Invoke(null, Array.Empty<object>());
            if (result is Task task)
                task.Wait();
        }
    }
}
