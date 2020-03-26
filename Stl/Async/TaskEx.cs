using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public static class TaskEx
    {
        public static T ResultOrThrow<T>(this Task<T> task) =>
            task.IsCompleted ? task.Result : throw Errors.TaskIsNotCompleted();

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
    }
}
