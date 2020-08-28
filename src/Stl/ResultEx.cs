using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Reflection;

namespace Stl
{
    public static class ResultEx
    {
        // AsTask & AsValueTask

        public static Task<T> AsTask<T>(this Result<T> result)
            => result.IsValue(out var value, out var error)
                ? Task.FromResult(value)
                : Task.FromException<T>(error);

        public static Task<T> AsTask<T>(this IResult<T> result)
            => result.IsValue(out var value, out var error)
                ? Task.FromResult(value)
                : Task.FromException<T>(error);

        public static ValueTask<T> AsValueTask<T>(this Result<T> result)
            => result.IsValue(out var value, out var error)
                ? ValueTaskEx.FromResult(value)
                : ValueTaskEx.FromException<T>(error);

        public static ValueTask<T> AsValueTask<T>(this IResult<T> result)
            => result.IsValue(out var value, out var error)
                ? ValueTaskEx.FromResult(value)
                : ValueTaskEx.FromException<T>(error);

        // Update (for IMutableResult<T>)

        public static void Update<T>(this IMutableResult<T> result, Func<Result<T>, Result<T>> updater)
            => result.Update(updater.Invoke(result.AsResult()));
        public static void Update<T>(this IMutableResult<T> result, Func<T, T> updater)
            => result.Update(updater.Invoke(result.Value));
        public static void Update<T>(this IMutableResult<T> result, Action<T> updater)
        {
            var clone = MemberwiseCloner.Clone(result.Value);
            updater.Invoke(clone);
            result.Update(clone);
        }

        // ThrowIfError

        public static void ThrowIfError<T>(this Result<T> result)
        {
            if (result.Error != null)
                ExceptionDispatchInfo.Capture(result.Error).Throw();
        }

        public static void ThrowIfError<T>(this IResult<T> result)
        {
            if (result.Error != null)
                ExceptionDispatchInfo.Capture(result.Error).Throw();
        }
    }
}
