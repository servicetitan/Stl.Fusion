using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Stl.Requirements;

public readonly record struct ExceptionBuilder
{
    public static Func<string, Exception> DefaultExceptionFactory { get; set; }
        = message => new ValidationException(message);
    public static string DefaultMessageTemplate { get; set; }
        = "{0}: validation failed.";

    public string? MessageTemplate { get; init; }
    public string? TargetName { get; init; }
    public Delegate? ExceptionFactory { get; init; }

    public ExceptionBuilder(Func<Exception>? exceptionFactory = null)
        : this(null!, null!, null!)
    {
        ExceptionFactory = exceptionFactory;
    }

    public ExceptionBuilder(string messageTemplate, Func<string, Exception>? exceptionFactory = null)
        : this(messageTemplate, null!, exceptionFactory) { }
    public ExceptionBuilder(string messageTemplate, string targetName, Func<string, Exception>? exceptionFactory = null)
    {
        MessageTemplate = messageTemplate;
        TargetName = targetName;
        ExceptionFactory = exceptionFactory;
    }

    public static implicit operator ExceptionBuilder(string messageTemplate)
        => new(messageTemplate);
    public static implicit operator ExceptionBuilder(Func<Exception> exceptionFactory)
        => new(null!, _ => exceptionFactory.Invoke());

    public Exception Build<TValue>(TValue? value)
    {
        var targetName = TargetName.NullIfEmpty() ?? typeof(TValue?).GetName();
        var message = MessageTemplate.NullIfEmpty() ?? DefaultMessageTemplate;
        var exception = ExceptionFactory switch {
            Func<string, Exception> messageBased => messageBased.Invoke(
                string.Format(CultureInfo.InvariantCulture, message, targetName, value)),
            Func<Exception> simple => simple.Invoke(),
            _ => DefaultExceptionFactory.Invoke(
                string.Format(CultureInfo.InvariantCulture, message, targetName, value)),
        };
        return exception;
    }
}
