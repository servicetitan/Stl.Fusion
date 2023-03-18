using System.Diagnostics.CodeAnalysis;

namespace Stl;

public static class RequirementTargetExt
{
    // Normal overloads

#if NETSTANDARD2_0
    public static T Require<T>(this T? target, Requirement<T>? requirement = null)
        where T : IRequirementTarget
#else
    public static T Require<T>([NotNull] this T? target, Requirement<T>? requirement = null)
        where T : IRequirementTarget
#endif
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

#if NETSTANDARD2_0
    public static T Require<T>(this T? target, Func<Requirement<T>> requirementBuilder)
        where T : IRequirementTarget
#else
    public static T Require<T>([NotNull] this T? target, Func<Requirement<T>> requirementBuilder)
        where T : IRequirementTarget
#endif
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
