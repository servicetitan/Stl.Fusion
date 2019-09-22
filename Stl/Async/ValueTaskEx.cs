using System.Threading.Tasks;

namespace Stl.Async
{
    public static class ValueTaskEx
    {
        public static ValueTask CompletedTask { get;  } = Task.CompletedTask.ToValueTask();
        public static ValueTask<bool> TrueTask { get;  } = New(true);
        public static ValueTask<bool> FalseTask { get;  } = New(false);
        
        public static ValueTask<T> New<T>(T value) => new ValueTask<T>(value);
    }
}
