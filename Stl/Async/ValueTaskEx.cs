using System.Threading.Tasks;

namespace Stl.Async
{
    public static class ValueTaskEx
    {
        public static ValueTask Completed { get;  } = Task.CompletedTask.ToValueTask();
        public static ValueTask<bool> True { get;  } = New(true);
        public static ValueTask<bool> False { get;  } = New(false);
        
        public static ValueTask<T> New<T>(T value) => new ValueTask<T>(value);
    }
}