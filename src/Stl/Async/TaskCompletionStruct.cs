using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public readonly struct TaskCompletionStruct<T> : IEquatable<TaskCompletionStruct<T>>
    {
        [ThreadStatic]
        private static volatile TaskCompletionSource<T>? _taskCompletionSource;
        private static readonly Func<Task<T>> CreateTask0;
        private static readonly Func<object?, TaskCreationOptions, Task<T>> CreateTask2;
        private static readonly Action<TaskCompletionSource<T>, Task<T>> TcsSetTask;

        public static TaskCompletionStruct<T> Empty => default;

        public readonly Task<T> Task;
        public bool IsValid => Task != null;
        public bool IsEmpty => Task == null;

        public TaskCompletionStruct(Task<T> task) 
            => Task = task;
        public TaskCompletionStruct(object? state, TaskCreationOptions taskCreationOptions)
            => Task = CreateTask2(state, taskCreationOptions);
        public TaskCompletionStruct(TaskCreationOptions taskCreationOptions)
            => Task = CreateTask2(null, taskCreationOptions);

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

        public bool TrySetCanceled(CancellationToken cancellationToken = default) 
            => Wrap(Task).TrySetCanceled(cancellationToken);
        public void SetCanceled() 
            => Wrap(Task).SetCanceled();

        // Type initializer

        static TaskCompletionStruct()
        {
            var tTcs = typeof(TaskCompletionSource<T>);
            var tTask = typeof(Task<T>);
            var fTask = tTcs.GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic);
            var pState = Expression.Parameter(typeof(object), "state");
            var pTco = Expression.Parameter(typeof(TaskCreationOptions), "taskCreationOptions");
            var pTcs = Expression.Parameter(tTcs, "tcs");
            var pTask = Expression.Parameter(tTask, "task");
            var pResult = Expression.Parameter(typeof(T), "result");
            var pException = Expression.Parameter(typeof(Exception), "exception");
            var pCancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var privateCtorBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance;

            var _taskCtor0 = tTask.GetConstructor(privateCtorBindingFlags, null, 
                Array.Empty<Type>(), null); 
            var _taskCtor2 = tTask.GetConstructor(privateCtorBindingFlags, null, 
                new [] {typeof(object), typeof(TaskCreationOptions)}, null); 
            CreateTask0 = Expression.Lambda<Func<Task<T>>>(
                Expression.New(_taskCtor0)).Compile();
            CreateTask2 = Expression.Lambda<Func<object?, TaskCreationOptions, Task<T>>>(
                Expression.New(_taskCtor2, pState, pTco), pState, pTco).Compile();

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

        // Equality

        public bool Equals(TaskCompletionStruct<T> other) 
            => Task.Equals(other.Task);
        public override bool Equals(object? obj) 
            => obj is TaskCompletionStruct<T> other && Equals(other);
        public override int GetHashCode() 
            => Task.GetHashCode();
        public static bool operator ==(TaskCompletionStruct<T> left, TaskCompletionStruct<T> right) 
            => left.Equals(right);
        public static bool operator !=(TaskCompletionStruct<T> left, TaskCompletionStruct<T> right) 
            => !left.Equals(right);
    }
}
