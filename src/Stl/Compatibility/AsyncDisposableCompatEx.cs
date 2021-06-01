using System;
using System.Threading.Tasks;
using Stl.Async;

namespace System
{
    public struct AsyncDisposableCompatWrapper<T> : IAsyncDisposable
#if !NETSTANDARD2_0    
        where T : IAsyncDisposable
#else    
        where T : IDisposable
#endif
    {
        public T? Component { get; }
        
        public ValueTask DisposeAsync()
        {
#if !NETSTANDARD2_0               
            return Component?.DisposeAsync() ?? ValueTaskEx.CompletedTask;
#else
            Component?.Dispose();
            return ValueTaskEx.CompletedTask;
#endif
        }

        public AsyncDisposableCompatWrapper(T? component)
        {
            this.Component = component;
        }
    }
    
    public static class AsyncDisposableCompatEx
    {
        /// <summary>
        /// Use only to avoid conditional compilation directives in parts where component in one of legacy targets does not supports IAsyncDisposable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static AsyncDisposableCompatWrapper<T> AsAsyncDisposable<T>(this T? component)
#if !NETSTANDARD2_0
            where T : IAsyncDisposable
#else
            where T : IDisposable
#endif
        {
            return new AsyncDisposableCompatWrapper<T>(component);
        }
    }
}
