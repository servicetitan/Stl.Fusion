using System;
using System.Threading.Tasks;

namespace Stl
{
    public static class ResultEx
    {
        public static Result<T> InvokeForResult<T, TState>(this Func<TState, T> func, TState state) 
        {
            try {
                return Result.Value(func.Invoke(state));
            }
            catch (Exception e) {
                return Result.Error<T>(e);
            }
        }

        public static Result<T> InvokeForResult<T>(this Func<T> func) 
        {
            try {
                return Result.Value(func.Invoke());
            }
            catch (Exception e) {
                return Result.Error<T>(e);
            }
        }

        public static Func<Result<T>> ToResultFunc<T>(this Func<T> func) => () => {
            try {
                return Result.Value(func.Invoke());
            }
            catch (Exception e) {
                return Result.Error<T>(e);
            }
        };

        public static Func<TState, Result<T>> ToResultFunc<TState, T>(this Func<TState, T> func) => state => {
            try {
                return Result.Value(func.Invoke(state));
            }
            catch (Exception e) {
                return Result.Error<T>(e);
            }
        };

        public static Task<Result<T>> ToResultTask<T>(this Task<T> task) => 
            task.ContinueWith(t => t.IsCompletedSuccessfully 
                ? Result.Value(t.Result) 
                : Result.Error<T>(t.Exception));
    }
}