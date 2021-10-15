using System.ComponentModel;
using System.Reflection;
using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class MethodExTest
{
    public interface IMethodA
    {
        [DisplayName("IMethodA")]
        void MethodA();
    }

    public interface IMethodV
    {
        [DisplayName("IMethodV")]
        void MethodV();
    }

    public interface IMethodG
    {
        [DisplayName("IMethodG")]
        void MethodG<T1>();
    }

    public interface IMethodX : IMethodA
    {
        void MethodX();
    }

    public abstract class A<T> : IMethodX
    {
        public abstract void MethodA();
        public virtual void MethodV() {}
        public abstract void MethodG<T1>();
        public void MethodX() {}
    }

    public class B : A<string>, IMethodV
    {
        [DisplayName("B")]
        public override void MethodA() {}
        public override void MethodV() {}
        [DisplayName("B")]
        public override void MethodG<T1>() {}
    }

    public class C : B, IMethodG
    {
        public override void MethodA() {}
    }

    public class D : C
    {
        [DisplayName("D")]
        public new void MethodA() {}
        [DisplayName("D")]
        public new virtual void MethodV() {}
    }

    public class E : D, IMethodA, IMethodV { }

    [Fact]
    public void GetBaseOrDeclaringMethodTest()
    {
        var methodAName = nameof(A<object>.MethodA);
        var methodVName = nameof(A<object>.MethodV);
        var methodGName = nameof(A<object>.MethodG);
        var tA = typeof(A<>);
        var tAString = tA.MakeGenericType(typeof(string));
        var tB = typeof(B);
        var tC = typeof(C);
        var tD = typeof(D);
        var tE = typeof(E);
        var tIMethodA = typeof(IMethodA);
        var tIMethodV = typeof(IMethodV);
        var tIMethodG = typeof(IMethodG);
        var tIMethodX = typeof(IMethodX);

        void Check(MethodInfo? method, Type? baseType)
        {
            var baseMethod = method!.GetBaseOrDeclaringMethod();
            var actualBaseType = baseMethod?.ReflectedType;
            actualBaseType.Should().BeSameAs(baseType);
        }

        var name = methodAName;
        Check(tA.GetMethod(name), null);
        Check(tB.GetMethod(name), tAString);
        Check(tC.GetMethod(name), tB);
        Check(tD.GetMethod(name), null);
        Check(tE.GetMethod(name), null);

        name = methodVName;
        Check(tA.GetMethod(name), null);
        Check(tB.GetMethod(name), tAString);
        Check(tC.GetMethod(name), tB);
        Check(tD.GetMethod(name), null);
        Check(tE.GetMethod(name), tD);

        name = methodGName;
        Check(tA.GetMethod(name), null);
        Check(tB.GetMethod(name), tAString);
        Check(tC.GetMethod(name), tB);
        Check(tD.GetMethod(name), tB);
        Check(tE.GetMethod(name), tB);
        // Generic -> generic definition
        Check(tA.GetMethod(name)!.MakeGenericMethod(tA), tA);
        Check(tB.GetMethod(name)!.MakeGenericMethod(tA), tB);
        Check(tC.GetMethod(name)!.MakeGenericMethod(tA), tB);
        Check(tD.GetMethod(name)!.MakeGenericMethod(tA), tB);
        Check(tE.GetMethod(name)!.MakeGenericMethod(tA), tB);
    }

    [Fact]
    public void GetAttributesTest()
    {
        var methodAName = nameof(A<object>.MethodA);
        var methodVName = nameof(A<object>.MethodV);
        var methodGName = nameof(A<object>.MethodG);
        var methodXName = nameof(A<object>.MethodX);
        var tA = typeof(A<>);
        var tAString = tA.MakeGenericType(typeof(string));
        var tB = typeof(B);
        var tC = typeof(C);
        var tD = typeof(D);
        var tE = typeof(E);
        var tIMethodA = typeof(IMethodA);
        var tIMethodV = typeof(IMethodV);
        var tIMethodG = typeof(IMethodG);
        var tIMethodX = typeof(IMethodX);

        void Check(MethodInfo? method, params string[] expected)
        {
            var attrs = method!.GetAttributes<DisplayNameAttribute>(true, true);
            attrs.Count.Should().Be(expected.Length);
            foreach (var (a, s) in attrs.Zip(expected, (a, s) => (a, s)))
                a.DisplayName.Should().Be(s);
            var attr = method!.GetAttribute<DisplayNameAttribute>(true, true);
            if (attr == null)
                expected.Length.Should().Be(0);
            else
                attr.DisplayName.Should().Be(expected[0]);
        }

        var name = methodAName;
        Check(tIMethodA.GetMethod(name), tIMethodA.Name);
        Check(tA.GetMethod(name), tIMethodA.Name);
        Check(tB.GetMethod(name), tB.Name, tIMethodA.Name);
        Check(tC.GetMethod(name), tB.Name, tIMethodA.Name);
        Check(tD.GetMethod(name), tD.Name);
        Check(tE.GetMethod(name), tD.Name); // Exception: no interface method here

        name = methodVName;
        Check(tIMethodV.GetMethod(name), tIMethodV.Name);
        Check(tA.GetMethod(name));
        Check(tB.GetMethod(name), tIMethodV.Name);
        Check(tC.GetMethod(name), tIMethodV.Name);
        Check(tD.GetMethod(name), tD.Name);
        Check(tE.GetMethod(name), tD.Name); // Exception: no interface method here

        name = methodGName;
        Check(tIMethodG.GetMethod(name), tIMethodG.Name);
        Check(tA.GetMethod(name));
        Check(tB.GetMethod(name), tB.Name);
        Check(tC.GetMethod(name), tIMethodG.Name, tB.Name);
        Check(tD.GetMethod(name), tIMethodG.Name, tB.Name);
        Check(tE.GetMethod(name), tIMethodG.Name, tB.Name);

        name = methodXName;
        Check(tIMethodX.GetMethod(name));
        Check(tA.GetMethod(name));
        Check(tB.GetMethod(name));
        Check(tC.GetMethod(name));
        Check(tD.GetMethod(name));
        Check(tE.GetMethod(name));
    }
}
