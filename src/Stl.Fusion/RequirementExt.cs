using Stl.Fusion.Requirements;
using Stl.Requirements;

namespace Stl.Fusion;

public static class RequirementExt
{
    public static ToResultExceptionRequirement<T> ToResultException<T>(this Requirement<T> requirement) 
        => ReferenceEquals(requirement, NotNullOrDefaultRequirement<T>.Default) 
            ? ToResultExceptionRequirement<T>.Default 
            : new ToResultExceptionRequirement<T>(requirement);
}
