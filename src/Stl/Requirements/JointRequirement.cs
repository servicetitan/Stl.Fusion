namespace Stl.Requirements;

public record JointRequirement<T>(
    Requirement<T> Primary,
    Requirement<T> Secondary
    ) : Requirement<T>
{
    public override bool IsSatisfied(T? value)
        => Primary.IsSatisfied(value) && Secondary.IsSatisfied(value);

    public override T Check(T? value)
        => Secondary.Check(Primary.Check(value));
}
