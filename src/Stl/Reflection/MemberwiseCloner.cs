using System.Linq.Expressions;

namespace Stl.Reflection;

public static class MemberwiseCloner
{
    private static readonly Func<object, object> MemberwiseCloneFunc;

    static MemberwiseCloner()
    {
        var miMemberwiseClone = typeof(object)
            .GetMethod(nameof(MemberwiseClone), BindingFlags.Instance | BindingFlags.NonPublic)!;
        var eSource = Expression.Parameter(typeof(object), "source");
        var eBody = Expression.Call(eSource, miMemberwiseClone);
        MemberwiseCloneFunc = Expression.Lambda<Func<object, object>>(eBody, eSource).Compile();
    }

    public static T Invoke<T>(T source)
    {
        var oSource = (object?) source;
        if (oSource == null)
            return default!;
        return (T) MemberwiseCloneFunc!.Invoke(oSource);
    }
}
