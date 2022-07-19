namespace Stl;

public static class RequirementTargetExt
{
    // Normal overloads

    public static T Require<T>(this T? target, Requirement<T>? requirement = null)
        where T : IRequirementTarget
    {
        requirement ??= Requirement<T>.MustExist;
        return requirement.Check(target);
    }

    public static async Task<T> Require<T>(this Task<T?> targetSource, Requirement<T>? requirement = null)
        where T : IRequirementTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        requirement ??= Requirement<T>.MustExist;
        return requirement.Check(target);
    }

    public static async ValueTask<T> Require<T>(this ValueTask<T?> targetSource, Requirement<T>? requirement = null)
        where T : IRequirementTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        requirement ??= Requirement<T>.MustExist;
        return requirement.Check(target);
    }

    // Overloads accepting requirement builder

    public static T Require<T>(this T? target, Func<Requirement<T>> requirementBuilder)
        where T : IRequirementTarget
        => requirementBuilder.Invoke().Check(target);

    public static async Task<T> Require<T>(this Task<T?> targetSource, Func<Requirement<T>> requirementBuilder)
        where T : IRequirementTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        return requirementBuilder.Invoke().Check(target);
    }

    public static async ValueTask<T> Require<T>(this ValueTask<T?> targetSource, Func<Requirement<T>> requirementBuilder)
        where T : IRequirementTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        return requirementBuilder.Invoke().Check(target);
    }
}
