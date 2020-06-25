using System;
using System.Reactive;
using System.Runtime.CompilerServices;

namespace Stl
{
    public static class IgnoreEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ignore<T>(this T instance) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Unit IgnoreAsUnit<T>(this T instance) => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<Unit> ToUnitFunc(this Action action) => () => {
            action.Invoke();
            return default;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<T, Unit> ToUnitFunc<T>(this Action<T> action) => arg => {
            action.Invoke(arg);
            return default;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<Unit> ToUnitFunc<T>(this Action<T> action, T state) => () => {
            action.Invoke(state);
            return default;
        };
    }
}
