using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Stl.Requirements;

public record struct ExceptionBuilder
{
    public static Func<string, Exception> DefaultExceptionFactory { get; } = message => new ValidationException(message);

    private readonly Func<string, Exception>? _exceptionFactory;

    public Func<string, Exception> ExceptionFactory {
        get => _exceptionFactory ?? DefaultExceptionFactory;
        init => _exceptionFactory = value;
    }

    public string? MessageTemplate { get; init; }
    public string? TargetName { get; init; }

    public ExceptionBuilder(Func<string, Exception> exceptionFactory, string? messageTemplate = null, string? targetName = null)
    {
        _exceptionFactory = exceptionFactory;
        MessageTemplate = messageTemplate;
        TargetName = targetName;
    }

    public static implicit operator ExceptionBuilder(string messageTemplate)
        => new(null!, messageTemplate);
    public static implicit operator ExceptionBuilder((string MessageTemplate, Func<string, Exception> ExceptionFactory) args)
        => new(args.ExceptionFactory, args.MessageTemplate);
    public static implicit operator ExceptionBuilder(Func<string, Exception> exceptionFactory)
        => new(exceptionFactory);
    public static implicit operator ExceptionBuilder(Func<Exception> exceptionFactory)
        => new(_ => exceptionFactory.Invoke());

    public Exception Build<TValue>(TValue? value)
    {
        var targetName = TargetName ?? typeof(TValue?).GetName();
        var message = MessageTemplate ?? "{0}: validation failed.";
        return ExceptionFactory.Invoke(string.Format(CultureInfo.InvariantCulture, message, targetName, value));
    }
}
