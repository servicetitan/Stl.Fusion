using System.Diagnostics.CodeAnalysis;

namespace Stl.Requirements;

public record JointRequirement<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    (Requirement<T> Primary, Requirement<T> Secondary) : Requirement<T>
{
#if NETSTANDARD2_0
    public override bool IsSatisfied(T? value)
        => Primary.IsSatisfied(value) && Secondary.IsSatisfied(value);

    public override T Check(T? value)
        => Secondary.Check(Primary.Check(value));
#else
    public override bool IsSatisfied([NotNullWhen(true)] T? value)
        => Primary.IsSatisfied(value) && Secondary.IsSatisfied(value);

    public override T Check([NotNull] T? value)
        => Secondary.Check(Primary.Check(value));
#endif

}
