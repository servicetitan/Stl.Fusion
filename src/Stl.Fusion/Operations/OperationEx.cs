using Stl.Collections;

namespace Stl.Fusion.Operations
{
    public static class OperationEx
    {
        public static void CaptureItems(this IOperation operation, OptionSet source)
        {
            var items = operation.Items;
            foreach (var (key, value) in source.Items) {
                if (value is IOperationItem i)
                    items = items.Set(key, value);
            }
            if (items != operation.Items)
                operation.Items = items;
        }

        public static void RestoreItems(this IOperation operation, OptionSet target)
        {
            foreach (var (key, value) in operation.Items.Items)
                target[key] = value;
        }
    }
}
