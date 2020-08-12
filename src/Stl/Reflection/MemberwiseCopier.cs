using System;
using System.Reflection;

namespace Stl.Reflection
{
    public class MemberwiseCopier<T>
    {
        protected const BindingFlags PropertyOrFieldBindingFlagsMask =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public BindingFlags PropertyBindingFlags { get; set; } = BindingFlags.Instance | BindingFlags.Public;
        public BindingFlags FieldBindingFlags { get; set; } = 0;
        public Func<MemberInfo, bool>? Filter { get; set; }
        public T Source { get; set; } = default!;

        public MemberwiseCopier<T> Configure(Action<MemberwiseCopier<T>> configurer)
        {
            configurer.Invoke(this);
            return this;
        }

        public MemberwiseCopier<T> AddProperties(BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            PropertyBindingFlags |= bindingFlags & PropertyOrFieldBindingFlagsMask;
            return this;
        }

        public MemberwiseCopier<T> AddFields(BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            FieldBindingFlags |= bindingFlags & PropertyOrFieldBindingFlagsMask;
            return this;
        }

        public MemberwiseCopier<T> SetFilter(Func<MemberInfo, bool>? filter)
        {
            Filter = filter;
            return this;
        }

        public T Apply(T target)
        {
            var oSource = (object) Source!;
            var oTarget = (object) target!;
            var type = typeof(T);
            var fields = type.GetFields(FieldBindingFlags & PropertyOrFieldBindingFlagsMask);
            foreach (var field in fields) {
                if (!(Filter?.Invoke(field) ?? true))
                    continue;
                field.SetValue(oTarget, field.GetValue(oSource));
            }
            var properties = type.GetProperties(FieldBindingFlags & PropertyOrFieldBindingFlagsMask);
            foreach (var property in properties) {
                if (!(Filter?.Invoke(property) ?? true))
                    continue;
                property.SetValue(oTarget, property.GetValue(oSource));
            }
            return target;
        }
    }

    public static class MemberwiseCopier
    {
        public static MemberwiseCopier<T> New<T>(T source)
            => new MemberwiseCopier<T>() { Source = source };
    }
}
