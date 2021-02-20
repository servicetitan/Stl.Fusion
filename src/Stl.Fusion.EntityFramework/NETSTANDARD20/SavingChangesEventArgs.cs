#if NETSTANDARD2_0

using System;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Event arguments for the <see cref="E:Microsoft.EntityFrameworkCore.DbContext.SavingChanges" /> event.
    /// </summary>
    public class SavingChangesEventArgs : SaveChangesEventArgs
    {
        /// <summary>
        ///     Creates event arguments for the <see cref="M:DbContext.SavingChanges" /> event.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"> The value passed to SaveChanges. </param>
        public SavingChangesEventArgs(bool acceptAllChangesOnSuccess)
            : base(acceptAllChangesOnSuccess)
        {
        }
    }
    
    /// <summary>
    ///     Base event arguments for the <see cref="M:DbContext.SaveChanges" /> and <see cref="M:DbContext.SaveChangesAsync" /> events.
    /// </summary>
    public abstract class SaveChangesEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a base event arguments instance for <see cref="M:DbContext.SaveChanges" />
        ///     or <see cref="M:DbContext.SaveChangesAsync" /> events.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"> The value passed to SaveChanges. </param>
        protected SaveChangesEventArgs(bool acceptAllChangesOnSuccess) => this.AcceptAllChangesOnSuccess = acceptAllChangesOnSuccess;

        /// <summary>
        ///     The value passed to <see cref="M:DbContext.SaveChanges" /> or <see cref="M:DbContext.SaveChangesAsync" />.
        /// </summary>
        public virtual bool AcceptAllChangesOnSuccess { get; }
    }
}

#endif