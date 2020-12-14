// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
#if !NET5_0
    // See https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
    public class IsExternalInit { }
#endif
}
