using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class CancellationTokenEx
    {
        // Note that this method won't release the token unless it's cancelled!
        public static TaskCompletionSource<T> ToTaskCompletionSource<T>(this CancellationToken token, 
            bool throwIfCancelled,  
            TaskCreationOptions taskCreationOptions = default)
        {
            var tcs = new TaskCompletionSource<T>(taskCreationOptions);
            if (throwIfCancelled)
                token.Register(arg => {
                    var tcs1 = (TaskCompletionSource<T>) arg;
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
    }
}
