#if NETSTANDARD2_0

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Specifies an index to be generated in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IndexAttribute : Attribute
    {
        private bool? _isUnique;
        private string _name;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.EntityFrameworkCore.IndexAttribute" /> class.
        /// </summary>
        /// <param name="propertyNames"> The properties which constitute the index, in order (there must be at least one). </param>
        public IndexAttribute([CanBeNull] params string[] propertyNames)
        {
            Check.NotEmpty<string>((IReadOnlyList<string>) propertyNames, nameof (propertyNames));
            Check.HasNoEmptyElements((IReadOnlyList<string>) propertyNames, nameof (propertyNames));
            this.PropertyNames = (IReadOnlyList<string>) ((IEnumerable<string>) propertyNames).ToList<string>();
        }

        /// <summary>
        ///     The properties which constitute the index, in order.
        /// </summary>
        public IReadOnlyList<string> PropertyNames { get; }

        /// <summary>The name of the index.</summary>
        public string Name
        {
            get => this._name;
            [param: JetBrains.Annotations.NotNull] set => this._name = Check.NotNull<string>(value, nameof (value));
        }

        /// <summary>Whether the index is unique.</summary>
        public bool IsUnique
        {
            get => this._isUnique.GetValueOrDefault();
            set => this._isUnique = new bool?(value);
        }

        /// <summary>
        ///     Checks whether <see cref="P:Microsoft.EntityFrameworkCore.IndexAttribute.IsUnique" /> has been explicitly set to a value.
        /// </summary>
        public bool IsUniqueHasValue => this._isUnique.HasValue;
    }
}

namespace JetBrains.Annotations
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Delegate)]
  internal sealed class CanBeNullAttribute : Attribute
  {
  }
  
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Delegate)]
  internal sealed class NotNullAttribute : Attribute
  {
  }
  
  [AttributeUsage(AttributeTargets.Parameter)]
  internal sealed class InvokerParameterNameAttribute : Attribute
  {
  }
  
  [AttributeUsage(AttributeTargets.Parameter)]
  internal sealed class NoEnumerationAttribute : Attribute
  {
  }
  
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
  internal sealed class ContractAnnotationAttribute : Attribute
  {
      public string Contract { get; }

      public bool ForceFullStates { get; }

      public ContractAnnotationAttribute([NotNull] string contract)
          : this(contract, false)
      {
      }

      public ContractAnnotationAttribute([NotNull] string contract, bool forceFullStates)
      {
          this.Contract = contract;
          this.ForceFullStates = forceFullStates;
      }
  }
}

namespace Microsoft.EntityFrameworkCore.Utilities
{
  [DebuggerStepThrough]
  internal static class Check
  {
    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>([NoEnumeration] T value, [InvokerParameterName, JetBrains.Annotations.NotNull] string parameterName)
    {
      if ((object) value == null)
      {
        Check.NotEmpty(parameterName, nameof (parameterName));
        throw new ArgumentNullException(parameterName);
      }
      return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static IReadOnlyList<T> NotEmpty<T>(
      IReadOnlyList<T> value,
      [InvokerParameterName, JetBrains.Annotations.NotNull] string parameterName)
    {
      Check.NotNull<IReadOnlyList<T>>(value, parameterName);
      if (value.Count == 0)
      {
        Check.NotEmpty(parameterName, nameof (parameterName));
        throw new ArgumentException(AbstractionsStrings.CollectionArgumentIsEmpty((object) parameterName));
      }
      return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotEmpty(string value, [InvokerParameterName, JetBrains.Annotations.NotNull] string parameterName)
    {
      Exception exception = (Exception) null;
      if (value == null)
        exception = (Exception) new ArgumentNullException(parameterName);
      else if (value.Trim().Length == 0)
        exception = (Exception) new ArgumentException(AbstractionsStrings.ArgumentIsEmpty((object) parameterName));
      if (exception != null)
      {
        Check.NotEmpty(parameterName, nameof (parameterName));
        throw exception;
      }
      return value;
    }

    public static string NullButNotEmpty(string value, [InvokerParameterName, JetBrains.Annotations.NotNull] string parameterName)
    {
      switch (value)
      {
        case "":
          Check.NotEmpty(parameterName, nameof (parameterName));
          throw new ArgumentException(AbstractionsStrings.ArgumentIsEmpty((object) parameterName));
        default:
          return value;
      }
    }

    public static IReadOnlyList<T> HasNoNulls<T>(
      IReadOnlyList<T> value,
      [InvokerParameterName, JetBrains.Annotations.NotNull] string parameterName)
      where T : class
    {
      Check.NotNull<IReadOnlyList<T>>(value, parameterName);
      if (value.Any<T>((Func<T, bool>) (e => (object) e == null)))
      {
        Check.NotEmpty(parameterName, nameof (parameterName));
        throw new ArgumentException(parameterName);
      }
      return value;
    }

    public static IReadOnlyList<string> HasNoEmptyElements(
      IReadOnlyList<string> value,
      [InvokerParameterName, JetBrains.Annotations.NotNull] string parameterName)
    {
      Check.NotNull<IReadOnlyList<string>>(value, parameterName);
      if (value.Any<string>((Func<string, bool>) (s => string.IsNullOrWhiteSpace(s))))
      {
        Check.NotEmpty(parameterName, nameof (parameterName));
        throw new ArgumentException("CollectionArgumentHasEmptyElements", parameterName);
      }
      return value;
    }

    [Conditional("DEBUG")]
    public static void DebugAssert([DoesNotReturnIf(false)] bool condition, string message)
    {
      if (!condition)
        throw new Exception("Check.DebugAssert failed: " + message);
    }
  }
}

namespace System.Diagnostics.CodeAnalysis
{
  [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
  public sealed class DoesNotReturnIfAttribute : Attribute
  {
    public DoesNotReturnIfAttribute(bool parameterValue) => this.ParameterValue = parameterValue;

    public bool ParameterValue { get; }
  }
}


#endif