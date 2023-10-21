using System.Diagnostics.CodeAnalysis;

namespace Stl.Requirements;

public record ServiceExceptionWrapper<T>(Requirement<T> BaseRequirement) : Requirement<T>
{
    public static readonly ServiceExceptionWrapper<T> Default =
        new(MustExistRequirement<T>.Default);

    public override bool IsSatisfied(T? value)
        => BaseRequirement.IsSatisfied(value);

#if NETSTANDARD2_0
    public override T Check(T? value)
#else
    public override T Check([NotNull] T? value)
#endif
    {
        try {
            return BaseRequirement.Check(value);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ServiceException(e.Message, e);
        }
    }
}
