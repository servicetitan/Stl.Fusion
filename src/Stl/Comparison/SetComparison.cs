using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stl.Comparison
{
    public static class SetComparison
    {
        public static SetComparison<T> New<T>(
            ISet<T> left, 
            ISet<T> right)
            where T : notnull
            => new SetComparison<T>(left, right);

        public static SetComparison<T> New<T>(
            IEnumerable<T> left, 
            IEnumerable<T> right)
            where T : notnull
            => new SetComparison<T>(left.ToHashSet(), right.ToHashSet());
    }

    public class SetComparison<T>
        where T : notnull
    {
        public ISet<T> Left { get; }
        public ISet<T> Right { get; }

        public List<T> LeftOnly { get; }
        public List<T> RightOnly { get; }
        public List<T> Shared { get; }

        public bool AreCountsEqual => Left.Count == Right.Count;
        public bool AreEqual => Shared.Count == Left.Count && AreCountsEqual;

        public SetComparison(IEnumerable<T> left, IEnumerable<T> right) 
            : this(left.ToHashSet(), right.ToHashSet()) { }

        public SetComparison(ISet<T> left, ISet<T> right)
        {
            Left = left;
            Right = right;
            Shared = Left.Where(i => Right.Contains(i)).ToList();
            LeftOnly = Left.Where(i => !Right.Contains(i)).ToList();
            RightOnly = Right.Where(i => !Left.Contains(i)).ToList();
        }
    }
}
