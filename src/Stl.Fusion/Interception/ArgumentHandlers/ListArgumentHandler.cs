using Stl.Extensibility;

namespace Stl.Fusion.Interception;

[MatchFor(typeof(IReadOnlyList<>), typeof(ArgumentHandlerProvider))]
public class ListArgumentHandler<T> : ArgumentHandler
{
    public static ListArgumentHandler<T> Instance { get; } = new();

    protected ListArgumentHandler()
    {
        GetHashCodeFunc = o => {
            if (!(o is IReadOnlyList<T> list))
                return 0;
            switch (list.Count) {
            case 0:
                return -1;
            case 1:
                return HashCode.Combine(list[0]);
            case 2:
                return HashCode.Combine(list[0], list[1]);
            case 3:
                return HashCode.Combine(list[0], list[1], list[2]);
            case 4:
                return HashCode.Combine(list[0], list[1], list[2], list[3]);
            default:
                var hash = 0;
                var i = 0;
                var fastCount = list.Count - 3;
                for (; i < fastCount; i += 4)
                    hash = HashCode.Combine(hash, list[i], list[i+1], list[i+2], list[i+3]);
                for (; i < list.Count; i++)
                    hash = HashCode.Combine(hash, list[i]);
                return hash;
            }
        };
        EqualsFunc = (a, b) => {
            var aList = a as IReadOnlyList<T>;
            var bList = b as IReadOnlyList<T>;
            if (aList == null)
                return bList == null;
            if (bList == null)
                return false;
            if (aList.Count != bList.Count)
                return false;
            for (var i = 0; i < aList.Count; i++) {
                var aItem = aList[i];
                var bItem = bList[i];
                if (!EqualityComparer<T>.Default.Equals(aItem, bItem))
                    return false;
            }
            return true;
        };
    }
}
