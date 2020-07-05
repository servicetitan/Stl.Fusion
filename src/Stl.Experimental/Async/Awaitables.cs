using System.Runtime.CompilerServices;

namespace Stl.Async
{
    // These interfaces aren't really necessary -- they are here mostly to
    // describe the API needed to implement awaitables.
    
    public interface IAwaitable<out TAwaiter>
    {
        TAwaiter GetAwaiter();
    }
    
    public interface IAwaiter : INotifyCompletion, ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
    
    public interface IAwaiter<out T>
    {
        bool IsCompleted { get; }
        T GetResult();
    }
}
