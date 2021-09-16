using System;
using System.Linq.Expressions;
using System.Reflection;
using Stl.Internal;

namespace Stl.Reflection
{
    public static class ExpressionExt
    {
        public static (Type memberType, string memberName) MemberTypeAndName<T, TValue>(
            this Expression<Func<T, TValue>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            var memberExpression = expression.Body as MemberExpression;

            (Type memberType, string memberName) TypeAndName(MemberExpression me) =>
                (me.Member.ReturnType(), me.Member.Name);

            if (memberExpression != null)
                return TypeAndName(memberExpression);
            if (!(expression.Body is UnaryExpression body))
                throw Errors.ExpressionDoesNotSpecifyAMember(expression.ToString());
            memberExpression = body.Operand as MemberExpression;
            if (memberExpression == null)
                throw Errors.ExpressionDoesNotSpecifyAMember(expression.ToString());
            return TypeAndName(memberExpression);
        }

        public static Type ReturnType(this MemberInfo memberInfo) =>
            memberInfo switch {
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                FieldInfo fieldInfo => fieldInfo.FieldType,
                MethodInfo methodInfo => methodInfo.ReturnType,
                _ => throw Errors.UnexpectedMemberType(memberInfo.ToString()!)
            };
    }
}
