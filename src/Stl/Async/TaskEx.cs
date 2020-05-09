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
        public static readonly Task<Unit> InfiniteUnitTask;
        public static readonly TaskCompletionSource<Unit> UnitTaskCompletionSource;
        public static readonly Task<Unit> UnitTask = Task.FromResult(Unit.Default);
        public static readonly Task<bool> TrueTask = Task.FromResult(true);
        public static readonly Task<bool> FalseTask = Task.FromResult(false);

        static TaskEx()
        {
            InfiniteUnitTask = new TaskCompletionSource<Unit>().Task;
            var unitTcs = new TaskCompletionSource<Unit>();
            unitTcs.SetResult(default);
            UnitTaskCompletionSource = unitTcs;
        }

        // ToXxx

        public static ValueTask<T> ToValueTask<T>(this Task<T> source) => new ValueTask<T>(source);
        public static ValueTask ToValueTask(this Task source) => new ValueTask(source);

        // Note that this method won't release the token unless it's cancelled!
        public static TaskCompletionSource<T> ToTaskCompletionSource<T>(this CancellationToken token, 
            bool throwIfCancelled,  
            TaskCreationOptions taskCreationOptions = default)
        {
            var tcs = new TaskCompletionSource<T>(taskCreationOptions);
            if (throwIfCancelled)
                token.Register(arg => {
                    var tcs1 = (TaskCompletionSource<Unit>) arg;
                    tcs1.SetCanceled();
                }, tcs);
            else
                token.Register(arg => {
                    var tcs1 = (TaskCompletionSource<T>) arg;
                    tcs1.SetResult(default!);
                }, tcs);
            return tcs;
        }

        // Note that this method won't release the token unless it's cancelled!
        public static TaskCompletionSource<T> ToTaskCompletionSource<T>(this CancellationToken token, 
            T resultWhenCancelled,  
            TaskCreationOptions taskCreationOptions = default)
        {
            // ReSharper disable once HeapView.BoxingAllocation
            var tcs = new TaskCompletionSource<T>(resultWhenCancelled, taskCreationOptions);
            token.Register(arg => {
                var tcs1 = (TaskCompletionSource<T>) arg;
                tcs1.SetResult((T) tcs1.Task.AsyncState);
            }, tcs);
            return tcs;
        }

        // Note that this method won't release the token unless it's cancelled!
        public static TaskCompletionSource<T> ToTaskCompletionSource<T>(this CancellationToken token, 
            Exception exceptionWhenCancelled,  
            TaskCreationOptions taskCreationOptions = default)
        {
            var tcs = new TaskCompletionSource<T>(exceptionWhenCancelled, taskCreationOptions);
            token.Register(arg => {
                var tcs1 = (TaskCompletionSource<T>) arg;
                tcs1.SetException((Exception) tcs1.Task.AsyncState);
            }, tcs);
            return tcs;
        }

        // Note that this method won't release the token unless it's cancelled!
        public static Task<T> ToTask<T>(this CancellationToken token, bool throwIfCancelled, 
            TaskCreationOptions taskCreationOptions = default)
            => token.ToTaskCompletionSource<T>(throwIfCancelled, taskCreationOptions).Task;

        // Note that this method won't release the token unless it's cancelled!
        public static Task ToTask(this CancellationToken token, bool throwIfCancelled, 
            TaskCreationOptions taskCreationOptions = default)
            => token.ToTaskCompletionSource<Unit>(throwIfCancelled, taskCreationOptions).Task;

        // A safer version of the previous method relying on a secondary token
        public static async Task ToTask(this CancellationToken token, CancellationToken cancellationToken)
        {
            using var lts = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
            await lts.Token.ToTask(false).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        // WithFakeCancellation

        public static Task<T> WithFakeCancellation<T>(this Task<T> task, 
            CancellationToken cancellationToken)
        {
            if (cancellationToken == default)
                return task;

            async Task<T> InnerAsync() {
                var tokenTask = cancellationToken.ToTask<T>(true, task.CreationOptions);
                var winner = await Task.WhenAny(task, tokenTask);
                return await winner;
            }

            return InnerAsync();

        }

        public static Task WithFakeCancellation(this Task task, 
            CancellationToken cancellationToken)
        {
            if (cancellationToken == default)
                return task;

            async Task InnerAsync() {
                var tokenTask = cancellationToken.ToTask(true, task.CreationOptions);
                var winner = await Task.WhenAny(task, tokenTask);
                await winner;
            }

            return InnerAsync();
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
            var r1 = await task1.ConfigureAwait(false);
            var r2 = await task2.ConfigureAwait(false);
            return (r1, r2);
        }

        public static async Task<(T1, T2, T3)> Join<T1, T2, T3>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3)
        {
            var r1 = await task1.ConfigureAwait(false);
            var r2 = await task2.ConfigureAwait(false);
            var r3 = await task3.ConfigureAwait(false);
            return (r1, r2, r3);
        }

        public static async Task<(T1, T2, T3, T4)> Join<T1, T2, T3, T4>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
        {
            var r1 = await task1.ConfigureAwait(false);
            var r2 = await task2.ConfigureAwait(false);
            var r3 = await task3.ConfigureAwait(false);
            var r4 = await task4.ConfigureAwait(false);
            return (r1, r2, r3, r4);
        }

        public static async Task<(T1, T2, T3, T4, T5)> Join<T1, T2, T3, T4, T5>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5)
        {
            var r1 = await task1.ConfigureAwait(false);
            var r2 = await task2.ConfigureAwait(false);
            var r3 = await task3.ConfigureAwait(false);
            var r4 = await task4.ConfigureAwait(false);
            var r5 = await task5.ConfigureAwait(false);
            return (r1, r2, r3, r4, r5);
        }

        // WhileRunning

        public static async Task WhileRunning(this Task dependency, Func<CancellationToken, Task> dependentTaskFactory)
        {
            using var cts = new CancellationTokenSource();
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
            using var cts = new CancellationTokenSource();
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
