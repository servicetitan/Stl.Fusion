using Stl.CommandR.Internal;

namespace Stl.CommandR;

public static class CommandExt
{
    private static readonly ConcurrentDictionary<Type, Type> ResultTypeCache = new();
    private static readonly Type CommandWithResultType = typeof(ICommand<>);

    public static Type GetResultType(this ICommand command)
        => GetResultType(command.GetType());

    public static Type GetResultType(Type commandType)
    {
        if (commandType == null)
            throw new ArgumentNullException(nameof(commandType));
        var result = ResultTypeCache.GetOrAdd(commandType, tCommand => {
            foreach (var tInterface in tCommand.GetInterfaces()) {
                if (!tInterface.IsConstructedGenericType)
                    continue;
                var gInterface = tInterface.GetGenericTypeDefinition();
                if (gInterface != CommandWithResultType)
                    continue;
                return tInterface.GetGenericArguments()[0];
            }
            return null!;
        });
        return result ?? throw Errors.CommandMustImplementICommandOfTResult(commandType);
    }
}
