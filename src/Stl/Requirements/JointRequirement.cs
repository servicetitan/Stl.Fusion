namespace Stl.Requirements;

public record JointRequirement<T>(
    Requirement<T> Primary, 
    Requirement<T> Secondary
    ) : Requirement<T>
{
    public override bool IsSatisfied(T? value)
        => Primary.IsSatisfied(value) && Secondary.IsSatisfied(value);

    public override T Require(T? value)
        => Secondary.Require(Primary.Require(value));
}
