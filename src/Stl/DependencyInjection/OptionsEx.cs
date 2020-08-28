using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    public interface IOptions { }

    public static class OptionsEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOptions OrDefault<TOptions>(this TOptions? options)
            where TOptions : class, IOptions, new()
            => options ?? new TOptions();

        public static TOptions OrDefault<TOptions>(this TOptions? options, IServiceProvider services)
            where TOptions : class, IOptions, new()
        {
            if (options != null)
                return options;
            options = services.GetService<TOptions>();
            return options ?? new TOptions();
        }
    }
}
