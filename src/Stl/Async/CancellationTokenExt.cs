using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class CancellationTokenExt
    {
        public static CancellationTokenSource LinkWith(this CancellationToken token1, CancellationToken token2)
            => CancellationTokenSource.CreateLinkedTokenSource(token1, token2);

        // ToTask (typed)

        public static Disposable<Task<T>, CancellationTokenRegistration> ToTask<T>(
            this CancellationToken token,
            TaskCreationOptions taskCreationOptions = default)
        {
            var ts = TaskSource.New<T>(taskCreationOptions);
            var r = token.Register(arg => TaskSource.For((Task<T>) arg!).SetCanceled(), ts.Task);
#if NETSTANDARD
            return Disposable.New(ts.Task, r, (_, r1) => r1.Dispose());
#else
            return Disposable.New(ts.Task, r, (_, r1) => r1.Unregister());
#endif
        }

        public static Disposable<Task<T>, CancellationTokenRegistration> ToTask<T>(
            this CancellationToken token,
            T resultWhenCancelled,
            TaskCreationOptions taskCreationOptions = default)
        {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            var ts = TaskSource.New<T>(resultWhenCancelled, taskCreationOptions);
            var r = token.Register(arg => {
                var ts1 = TaskSource.For((Task<T>) arg!);
                ts1.SetResult((T) ts1.Task.AsyncState!);
            }, ts.Task);
#if NETSTANDARD
            return Disposable.New(ts.Task, r, (_, r1) => r1.Dispose());
#else
            return Disposable.New(ts.Task, r, (_, r1) => r1.Unregister());
#endif
        }

        public static Disposable<Task<T>, CancellationTokenRegistration> ToTask<T>(
            this CancellationToken token,
            Exception exceptionWhenCancelled,
            TaskCreationOptions taskCreationOptions = default)
        {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            var ts = TaskSource.New<T>(exceptionWhenCancelled, taskCreationOptions);
            var r = token.Register(arg => {
                var ts1 = TaskSource.For((Task<T>) arg!);
                ts1.SetException((Exception) ts1.Task.AsyncState!);
            }, ts.Task);
#if NETSTANDARD
            return Disposable.New(ts.Task, r, (_, r1) => r1.Dispose());
#else
            return Disposable.New(ts.Task, r, (_, r1) => r1.Unregister());
#endif
        }

        // ToTask (untyped)

        public static Disposable<Task, CancellationTokenRegistration> ToTask(
            this CancellationToken token,
            TaskCreationOptions taskCreationOptions = default)
        {
            var ts = TaskSource.New<Unit>(taskCreationOptions);
            var r = token.Register(arg => TaskSource.For((Task<Unit>) arg!).SetCanceled(), ts.Task);
#if NETSTANDARD
            return Disposable.New((Task) ts.Task, r, (_, r1) => r1.Dispose());
#else
            return Disposable.New((Task) ts.Task, r, (_, r1) => r1.Unregister());
#endif
        }

        public static Disposable<Task, CancellationTokenRegistration> ToTask(
            this CancellationToken token,
            Exception exceptionWhenCancelled,
            TaskCreationOptions taskCreationOptions = default)
        {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            var ts = TaskSource.New<Unit>(exceptionWhenCancelled, taskCreationOptions);
            var r = token.Register(arg => {
                var ts1 = TaskSource.For((Task<Unit>) arg!);
                ts1.SetException((Exception) ts1.Task.AsyncState!);
            }, ts.Task);
#if NETSTANDARD
            return Disposable.New((Task) ts.Task, r, (_, r1) => r1.Dispose());
#else
            return Disposable.New((Task) ts.Task, r, (_, r1) => r1.Unregister());
#endif
        }
    }
}
