namespace Stl.Interception.Interceptors;

public class TypeView<TView>
    where TView : class
{ }

public class TypeView<TImplementation, TView> : TypeView<TView>
    where TView : class
    where TImplementation : class
{ }
