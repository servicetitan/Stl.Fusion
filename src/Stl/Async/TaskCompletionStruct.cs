using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public readonly struct TaskCompletionStruct<T>
    {
        [ThreadStatic]
        private static volatile TaskCompletionSource<T>? _taskCompletionSource;
        private static readonly Func<Task<T>> CreateTask;
        private static readonly Action<TaskCompletionSource<T>, Task<T>> TcsSetTask;

        public readonly Task<T> Task;

        public static TaskCompletionStruct<T> New()
            => new TaskCompletionStruct<T>(CreateTask());

        private TaskCompletionStruct(Task<T> task) 
            => Task = task;

        // Private methods
        
        private static TaskCompletionSource<T> Wrap(Task<T> task)
        {
            var tcs = _taskCompletionSource;
            if (tcs == null) {
                tcs = new TaskCompletionSource<T>();
                var oldTcs = Interlocked.CompareExchange(ref _taskCompletionSource, tcs, null);
                if (oldTcs != null)
                    tcs = oldTcs;
            }
            TcsSetTask.Invoke(tcs, task);
            return tcs;
        }

        public bool TrySetResult(T result) 
            => Wrap(Task).TrySetResult(result);
        public void SetResult(T result) 
            => Wrap(Task).SetResult(result);

        public bool TrySetException(Exception exception) 
            => Wrap(Task).TrySetException(exception);
        public void SetException(Exception exception) 
            => Wrap(Task).SetException(exception);

        public bool TrySetCancelled(CancellationToken cancellationToken = default) 
            => Wrap(Task).TrySetCanceled(cancellationToken);
        public void SetCancelled() 
            => Wrap(Task).SetCanceled();

        // Type initializer

        static TaskCompletionStruct()
        {
            var tTcs = typeof(TaskCompletionSource<T>);
            var tTask = typeof(Task<T>);
            var fTask = tTcs.GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic);
            var pTcs = Expression.Parameter(tTcs, "tcs");
            var pTask = Expression.Parameter(tTask, "task");
            var pResult = Expression.Parameter(typeof(T), "result");
            var pException = Expression.Parameter(typeof(Exception), "exception");
            var pCancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var privateCtorBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance;

            var _taskCtor = tTask.GetConstructor(privateCtorBindingFlags, null, Array.Empty<Type>(), null); 
            CreateTask = Expression.Lambda<Func<Task<T>>>(
                Expression.New(_taskCtor)).Compile();

            // Creating assign expression via reflection b/c otherwise
            // it fails "lvalue must be writeable" check -- well,
            // obviously, because we're assigning a read-only field value here.
            var exampleAssign = Expression.Assign(pTask, pTask);
            var realAssign = (Expression) Activator.CreateInstance(
                exampleAssign.GetType(), 
                privateCtorBindingFlags, null,
                new object[] {Expression.Field(pTcs, fTask), pTask}, null);
            TcsSetTask = Expression.Lambda<Action<TaskCompletionSource<T>, Task<T>>>(
                realAssign, pTcs, pTask).Compile();
        }
    }
}
