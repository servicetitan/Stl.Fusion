using System;
using System.Linq.Expressions;
using System.Reflection;
using Stl.Internal;

namespace Stl.Reflection
{
    public static class ExpressionEx
    {
        public static (Type memberType, string memberName) MemberTypeAndName<T, TValue>(
            this Expression<Func<T, TValue>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof (expression));
            var memberExpression = expression.Body as MemberExpression;

            (Type memberType, string memberName) TypeAndName() => 
                (memberExpression.Member.ReturnType(), memberExpression.Member.Name);

            if (memberExpression != null)
                return TypeAndName();
            if (!(expression.Body is UnaryExpression body))
                throw Errors.ExpressionDoesNotSpecifyAMember(expression.ToString());
            memberExpression = body.Operand as MemberExpression;
            if (memberExpression == null)
                throw Errors.ExpressionDoesNotSpecifyAMember(expression.ToString());
            return TypeAndName();
        }

        public static Type ReturnType(this MemberInfo memberInfo) =>
            memberInfo switch {
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                FieldInfo fieldInfo => fieldInfo.FieldType,
                MethodInfo methodInfo => methodInfo.ReturnType,
                _ => throw Errors.UnexpectedMemberType(memberInfo.ToString())
            };
    }
}
