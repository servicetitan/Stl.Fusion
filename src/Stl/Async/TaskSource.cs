using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class TaskSource
    {
        public static TaskSource<T> For<T>(Task<T> task)
            => new(task);
        public static TaskSource<T> New<T>(bool runContinuationsAsynchronously)
            => runContinuationsAsynchronously
                ? new TaskSource<T>(TaskSource<T>.CreateTask2(null, TaskCreationOptions.RunContinuationsAsynchronously))
                : new TaskSource<T>(TaskSource<T>.CreateTask0());
        public static TaskSource<T> New<T>(object? state, TaskCreationOptions taskCreationOptions)
            => new(TaskSource<T>.CreateTask2(state, taskCreationOptions));
        public static TaskSource<T> New<T>(TaskCreationOptions taskCreationOptions)
            => new(TaskSource<T>.CreateTask2(null, taskCreationOptions));
    }

    public readonly struct TaskSource<T> : IEquatable<TaskSource<T>>
    {
        [ThreadStatic]
        private static volatile TaskCompletionSource<T>? _taskCompletionSource;
        internal static readonly Func<Task<T>> CreateTask0;
        internal static readonly Func<object?, TaskCreationOptions, Task<T>> CreateTask2;
        private static readonly Action<TaskCompletionSource<T>, Task<T>> SetTask;

        public static TaskSource<T> Empty => default;

        public readonly Task<T> Task;

        public bool IsEmpty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Task == null!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TaskSource(Task<T> task) => Task = task;

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
            SetTask.Invoke(tcs, task);
            return tcs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result)
            => Wrap(Task).TrySetResult(result);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
            => Wrap(Task).SetResult(result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
            => Wrap(Task).TrySetException(exception);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
            => Wrap(Task).SetException(exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
            => Wrap(Task).TrySetCanceled(cancellationToken);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCanceled()
            => Wrap(Task).SetCanceled();

        // Type initializer

        static TaskSource()
        {
            var tTcs = typeof(TaskCompletionSource<T>);
            var tTask = typeof(Task<T>);
#if !NETSTANDARD2_0            
            var fTask = tTcs.GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic);
#else
            var fTask = tTcs.GetField("m_task", BindingFlags.Instance | BindingFlags.NonPublic);
#endif
            var pState = Expression.Parameter(typeof(object), "state");
            var pTco = Expression.Parameter(typeof(TaskCreationOptions), "taskCreationOptions");
            var pTcs = Expression.Parameter(tTcs, "tcs");
            var pTask = Expression.Parameter(tTask, "task");
            var privateCtorBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance;

            var _taskCtor0 = tTask.GetConstructor(privateCtorBindingFlags, null,
                Array.Empty<Type>(), null);
            var _taskCtor2 = tTask.GetConstructor(privateCtorBindingFlags, null,
                new [] {typeof(object), typeof(TaskCreationOptions)}, null);
            CreateTask0 = Expression.Lambda<Func<Task<T>>>(
                Expression.New(_taskCtor0!)).Compile();
            CreateTask2 = Expression.Lambda<Func<object?, TaskCreationOptions, Task<T>>>(
                Expression.New(_taskCtor2!, pState, pTco), pState, pTco).Compile();

#if !NETSTANDARD2_0
            // Creating assign expression via reflection b/c otherwise
            // it fails "lvalue must be writeable" check -- well,
            // obviously, because we're assigning a read-only field value here.
            var exampleAssign = Expression.Assign(pTask, pTask);
            var realAssign = (Expression) Activator.CreateInstance(
                exampleAssign.GetType(),
                privateCtorBindingFlags, null,
                new object[] {Expression.Field(pTcs, fTask!), pTask}, null)!;
            SetTask = Expression.Lambda<Action<TaskCompletionSource<T>, Task<T>>>(
                realAssign, pTcs, pTask).Compile();
#else
            SetTask = GenerateSetFieldMethod(fTask);
#endif
        }
        
        private static Action<TaskCompletionSource<T>, Task<T>> GenerateSetFieldMethod(FieldInfo field)
        {
            var dm = new DynamicMethod(String.Concat ("_Set", field.Name, "_"), typeof(void),
                new Type[] { typeof(TaskCompletionSource<T>), typeof(Task<T>) },
                field.DeclaringType, true);
            ILGenerator generator = dm.GetILGenerator ();

            generator.Emit (OpCodes.Ldarg_0);
            generator.Emit (OpCodes.Ldarg_1);
            if (field.FieldType.IsValueType)
                generator.Emit (OpCodes.Unbox_Any, field.FieldType);
            generator.Emit (OpCodes.Stfld, field);
            generator.Emit (OpCodes.Ret);

            return (Action<TaskCompletionSource<T>, Task<T>>)dm.CreateDelegate (typeof(Action<TaskCompletionSource<T>, Task<T>>));
        }

        // Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TaskSource<T> other)
            => Task.Equals(other.Task);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
            => obj is TaskSource<T> other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => Task.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TaskSource<T> left, TaskSource<T> right)
            => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TaskSource<T> left, TaskSource<T> right)
            => !left.Equals(right);
    }
}
