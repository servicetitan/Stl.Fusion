using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    public static class HasDefaultEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static THasDefault OrDefault<THasDefault>(this THasDefault? hasDefault)
            where THasDefault : class, IHasDefault, new()
            => hasDefault ?? new THasDefault();

        public static THasDefault OrDefault<THasDefault>(this THasDefault? hasDefault, IServiceProvider services)
            where THasDefault : class, IHasDefault, new()
            => hasDefault ?? services.GetService<THasDefault>().OrDefault();
    }
}
