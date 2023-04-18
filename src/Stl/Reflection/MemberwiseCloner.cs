using System.Linq.Expressions;

namespace Stl.Reflection;

public static class MemberwiseCloner
{
    private static readonly Func<object, object> MemberwiseCloneFunc;

    static MemberwiseCloner()
    {
        MemberwiseCloneFunc = (Func<object, object>)typeof(object)
            .GetMethod(nameof(MemberwiseClone), BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate(typeof(Func<object, object>));
    }

    public static T Invoke<T>(T source)
    {
        var oSource = (object?) source;
        if (oSource == null)
            return default!;
        return (T) MemberwiseCloneFunc(oSource);
    }
}
