#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Fake IndexAttribute is used only to keep models the same for all targets.
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
        public IndexAttribute(params string[] propertyNames)
        {
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
            set => this._name = value;
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

#endif
