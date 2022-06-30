namespace Stl.Interception.Interceptors;

public class TypeView
{
    public object ViewTarget { get; set; } = default!;
}

public class TypeView<TView> : TypeView
    where TView : class
{ }

public class TypeView<TTarget, TView> : TypeView<TView>
    where TView : class
    where TTarget : class
{ }
