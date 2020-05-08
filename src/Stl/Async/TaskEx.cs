using System;
using System.Reactive;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public static class TaskEx
    {
        public static readonly TaskCompletionSource<Unit> UnitTaskCompletionSource;
        public static readonly Task<Unit> UnitTask = Task.FromResult(Unit.Default);
        public static readonly Task<bool> TrueTask = Task.FromResult(true);
        public static readonly Task<bool> FalseTask = Task.FromResult(false);

        static TaskEx()
        {
            var unitTcs = new TaskCompletionSource<Unit>();
            unitTcs.SetResult(default);
            UnitTaskCompletionSource = unitTcs;
        }

        // ToXxx

        public static ValueTask<T> ToValueTask<T>(this Task<T> source) => new ValueTask<T>(source);
        public static ValueTask ToValueTask(this Task source) => new ValueTask(source);

        // Note that this method won't release the token unless it's cancelled!
        public static Task ToTask(this CancellationToken token, bool throwIfCancelled) => 
            throwIfCancelled 
                ? Task.Delay(-1, token) 
                : Task.Delay(-1, token).ContinueWith(_ => {}, TaskContinuationOptions.OnlyOnCanceled);

        // A safer version of the previous method relying on a secondary token
        public static async Task ToTask(this CancellationToken token, CancellationToken cancellationToken)
        {
            using var lts = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
            await lts.Token.ToTask(false).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        // SuppressXxx

        public static Task SuppressExceptions(this Task task) 
            => task.ContinueWith(t => { });
        public static Task<T> SuppressExceptions<T>(this Task<T> task) 
            => task.ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : default!);
        
        public static Task SuppressCancellation(this Task task)
            => task.ContinueWith(t => {
                if (t.IsCompletedSuccessfully || t.IsCanceled)
                    return;
                ExceptionDispatchInfo.Throw(t.Exception!);
            });
        public static Task<T> SuppressCancellation<T>(this Task<T> task)
            => task.ContinueWith(t => !t.IsCanceled ? t.Result : default!);

        // Join

        public static async Task<(T1, T2)> Join<T1, T2>(
            Task<T1> task1, Task<T2> task2)
        {
            await Task.WhenAll(task1, task2).ConfigureAwait(false);
            return (task1.Result, task2.Result);
        }

        public static async Task<(T1, T2, T3)> Join<T1, T2, T3>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3)
        {
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            return (task1.Result, task2.Result, task3.Result);
        }

        public static async Task<(T1, T2, T3, T4, T5)> Join<T1, T2, T3, T4, T5>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5)
        {
            await Task.WhenAll(task1, task2, task3, task4, task5).ConfigureAwait(false);
            return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result);
        }

        public static async Task<(T1, T2, T3, T4)> Join<T1, T2, T3, T4>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
        {
            await Task.WhenAll(task1, task2, task3, task4).ConfigureAwait(false);
            return (task1.Result, task2.Result, task3.Result, task4.Result);
        }

        // WhileRunning

        public static async Task WhileRunning(this Task dependency, Func<CancellationToken, Task> dependentTaskFactory)
        {
            var cts = new CancellationTokenSource();
            var dependentTask = dependentTaskFactory.Invoke(cts.Token);
            try {
                await dependency.ConfigureAwait(false);
            }
            finally {
                cts.Cancel();
                await dependentTask.SuppressCancellation().ConfigureAwait(false);
            }
        }

        public static async Task<T> WhileRunning<T>(this Task<T> dependency, Func<CancellationToken, Task> dependentTaskFactory)
        {
            var cts = new CancellationTokenSource();
            var dependentTask = dependentTaskFactory.Invoke(cts.Token);
            try {
                return await dependency.ConfigureAwait(false);
            }
            finally {
                cts.Cancel();
                await dependentTask.SuppressCancellation().ConfigureAwait(false);
            }
        }

        // AssertXxx

        public static Task AssertCompleted(this Task task) 
            => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;
        
        public static Task<T> AssertCompleted<T>(this Task<T> task) 
            => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;
    }
}
