using System.ComponentModel.DataAnnotations;
using System.Security;

namespace Stl;

/// <summary>
/// Tells if an error is transient (might be gone on retry). 
/// </summary>
public interface ITransientErrorDetector
{
    bool IsTransient(Exception error);
}

/// <summary>
/// Tells if an error is transient (might be gone on retry). 
/// </summary>
/// <typeparam name="TContext">Typically the service that requires this detector.</typeparam>
public interface ITransientErrorDetector<TContext> : ITransientErrorDetector
{ }

/// <summary>
/// Abstract base class for <see cref="ITransientErrorDetector"/>-s.
/// </summary>
public abstract record TransientErrorDetector : ITransientErrorDetector
{
    /// <summary>
    /// This detector is used by Fusion's IComputed by default, see
    /// FusionBuilder's constructor to understand how to replace it in
    /// the DI container, or simply set this property to whatever you prefer
    /// before calling .AddFusion() for the first time.
    /// </summary>
    public static TransientErrorDetector DefaultPreferTransient { get; set; } = New(error => {
        return error switch {
            ValidationException => false,
            SecurityException => false,
            ServiceException => false,
            ArgumentException => false,
            _ => true,
        };
    });

    /// <summary>
    /// This detector is used by Fusion's OperationReprocessor by default, see
    /// FusionBuilder.AddOperationReprocessor to understand how to replace it in
    /// the DI container, or simply set this property to whatever you prefer
    /// before calling .AddFusion() for the first time.
    /// </summary>
    public static TransientErrorDetector DefaultPreferNonTransient { get; set; } = New(error => {
        return error switch {
            ITransientException => true,
            _ => false,
        };
    });

    public static TransientErrorDetector New(Func<Exception, bool> detector)
        => new FuncTransientErrorDetector(detector);

    public abstract bool IsTransient(Exception error);

    // Operators

    public static TransientErrorDetector operator &(TransientErrorDetector primary, ITransientErrorDetector secondary)
        => New(e => primary.IsTransient(e) && secondary.IsTransient(e));
    public static TransientErrorDetector operator |(TransientErrorDetector primary, ITransientErrorDetector secondary)
        => New(e => primary.IsTransient(e) || secondary.IsTransient(e));

    // Nested types

    private record FuncTransientErrorDetector(Func<Exception, bool> Detector) : TransientErrorDetector
    {
        public override bool IsTransient(Exception error)
            => Detector(error);
    }
}
