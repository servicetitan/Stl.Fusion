using System.Reflection;
using FluentAssertions.Execution;
using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class MemberInfoExtTest
{
    private static Task<bool> StaticTaskField = TaskExt.FalseTask;
    private static bool StaticBoolField;
    private static Task<bool> StaticTaskProperty { get; set; } = TaskExt.FalseTask;
    private static bool StaticBoolProperty { get; set; }

    private Task<bool> TaskField = TaskExt.FalseTask;
    private bool BoolField;
    private Task<bool> TaskProperty { get; set; } = TaskExt.FalseTask;
    private bool BoolProperty { get; set; }

    [Fact]
    public void BoolMemberTest()
    {
        var type = GetType();
        Test(type.GetProperty(nameof(BoolProperty), BindingFlags.NonPublic | BindingFlags.Instance)!);
        Test(type.GetField(nameof(BoolField), BindingFlags.NonPublic | BindingFlags.Instance)!);

        void Test(MemberInfo mi)
        {
            Action action = () => mi.GetGetter<object, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetGetter<object, bool>(true);
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, bool>(true);
            action.Should().Throw<InvalidCastException>();

            var getter = mi.GetGetter<MemberInfoExtTest, bool>();
            var untypedGetter = mi.GetGetter<object, object>(true);
            var setter = mi.GetSetter<MemberInfoExtTest, bool>();
            var untypedSetter = mi.GetSetter<object, object>(true);

            if (getter == null || untypedGetter == null || setter == null || untypedSetter == null) {
                getter.Should().NotBeNull();
                untypedGetter.Should().NotBeNull();
                setter.Should().NotBeNull();
                untypedSetter.Should().NotBeNull();
                throw new AssertionFailedException(
                    "We shouldn't get here - this throw is solely to suppress later nullability warnings.");
            }

            BoolProperty = false;
            BoolField = false;
            getter(this).Should().BeFalse();
            untypedGetter(this).Should().Be(false);

            BoolProperty = true;
            BoolField = true;
            getter(this).Should().BeTrue();
            untypedGetter(this).Should().Be(true);

            setter(this, false);
            (mi is FieldInfo ? BoolField : BoolProperty).Should().BeFalse();
            untypedSetter(this, true);
            (mi is FieldInfo ? BoolField : BoolProperty).Should().BeTrue();
        }
    }

    [Fact]
    public void StaticBoolMemberTest()
    {
        var type = GetType();
        Test(type.GetProperty(nameof(StaticBoolProperty), BindingFlags.NonPublic | BindingFlags.Static)!);
        Test(type.GetField(nameof(StaticBoolField), BindingFlags.NonPublic | BindingFlags.Static)!);

        void Test(MemberInfo mi)
        {
            Action action = () => mi.GetGetter<object, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetGetter<object, bool>(true);
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, bool>(true);
            action.Should().Throw<InvalidCastException>();

            var getter = mi.GetGetter<MemberInfoExtTest, bool>();
            var untypedGetter = mi.GetGetter<object, object>(true);
            var setter = mi.GetSetter<MemberInfoExtTest, bool>();
            var untypedSetter = mi.GetSetter<object, object>(true);

            if (getter == null || untypedGetter == null || setter == null || untypedSetter == null) {
                getter.Should().NotBeNull();
                untypedGetter.Should().NotBeNull();
                setter.Should().NotBeNull();
                untypedSetter.Should().NotBeNull();
                throw new AssertionFailedException(
                    "We shouldn't get here - this throw is solely to suppress later nullability warnings.");
            }

            StaticBoolProperty = false;
            StaticBoolField = false;
            getter(null!).Should().BeFalse();
            untypedGetter(null!).Should().Be(false);

            StaticBoolProperty = true;
            StaticBoolField = true;
            getter(null!).Should().BeTrue();
            untypedGetter(null!).Should().Be(true);

            setter(null!, false);
            (mi is FieldInfo ? StaticBoolField : StaticBoolProperty).Should().BeFalse();
            untypedSetter(null!, true);
            (mi is FieldInfo ? StaticBoolField : StaticBoolProperty).Should().BeTrue();
        }
    }

    [Fact]
    public void TaskMemberTest()
    {
        var type = GetType();
        Test(type.GetProperty(nameof(TaskProperty), BindingFlags.NonPublic | BindingFlags.Instance)!);
        Test(type.GetField(nameof(TaskField), BindingFlags.NonPublic | BindingFlags.Instance)!);

        void Test(MemberInfo mi)
        {
            Action action = () => mi.GetGetter<MemberInfoExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetGetter<MemberInfoExtTest, Task>(true);

            _ = mi.GetGetter<MemberInfoExtTest, object>();
            _ = mi.GetGetter<MemberInfoExtTest, Task>();
            _ = mi.GetGetter<MemberInfoExtTest, Task<bool>>();

            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, Task>();
            action.Should().Throw<InvalidCastException>();

            var action1 = () => mi.GetSetter<MemberInfoExtTest, Task>();
            action1.Should().Throw<InvalidCastException>();
            _ = mi.GetSetter<MemberInfoExtTest, Task<bool>>();
            _ = mi.GetSetter<MemberInfoExtTest, object>(true);
            _ = mi.GetSetter<MemberInfoExtTest, Task>(true);
            _ = mi.GetSetter<MemberInfoExtTest, Task<bool>>(true);

            var getter = mi.GetGetter<MemberInfoExtTest, Task>();
            var untypedGetter = mi.GetGetter<object, object>(true);
            var setter = mi.GetSetter<MemberInfoExtTest, Task<bool>>();
            var untypedSetter = mi.GetSetter<object, object>(true);

            if (getter == null || untypedGetter == null || setter == null || untypedSetter == null) {
                getter.Should().NotBeNull();
                untypedGetter.Should().NotBeNull();
                setter.Should().NotBeNull();
                untypedSetter.Should().NotBeNull();
                throw new AssertionFailedException(
                    "We shouldn't get here - this throw is solely to suppress later nullability warnings.");
            }

            var falseTask = Task.FromResult(false);
            var trueTask = Task.FromResult(true);

            TaskField = falseTask;
            TaskProperty = falseTask;
            getter(this).Should().BeSameAs(falseTask);
            untypedGetter(this).Should().BeSameAs(falseTask);

            TaskField = trueTask;
            TaskProperty = trueTask;
            getter(this).Should().BeSameAs(trueTask);
            untypedGetter(this).Should().BeSameAs(trueTask);

            setter(this, falseTask);
            (mi is FieldInfo ? TaskField : TaskProperty).Should().BeSameAs(falseTask);
            untypedSetter(this, trueTask);
            (mi is FieldInfo ? TaskField : TaskProperty).Should().BeSameAs(trueTask);
        }
    }

    [Fact]
    public void StaticTaskMemberTest()
    {
        var type = GetType();
        Test(type.GetProperty(nameof(StaticTaskProperty), BindingFlags.NonPublic | BindingFlags.Static)!);
        Test(type.GetField(nameof(StaticTaskField), BindingFlags.NonPublic | BindingFlags.Static)!);

        void Test(MemberInfo mi)
        {
            Action action = () => mi.GetGetter<MemberInfoExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetGetter<MemberInfoExtTest, Task>(true);

            _ = mi.GetGetter<MemberInfoExtTest, object>();
            _ = mi.GetGetter<MemberInfoExtTest, Task>();
            _ = mi.GetGetter<MemberInfoExtTest, Task<bool>>();

            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberInfoExtTest, Task>();
            action.Should().Throw<InvalidCastException>();

            var action1 = () => mi.GetSetter<MemberInfoExtTest, Task>();
            action1.Should().Throw<InvalidCastException>();
            _ = mi.GetSetter<MemberInfoExtTest, Task<bool>>();
            _ = mi.GetSetter<MemberInfoExtTest, object>(true);
            _ = mi.GetSetter<MemberInfoExtTest, Task>(true);
            _ = mi.GetSetter<MemberInfoExtTest, Task<bool>>(true);

            var getter = mi.GetGetter<MemberInfoExtTest, Task>();
            var untypedGetter = mi.GetGetter<object, object>(true);
            var setter = mi.GetSetter<MemberInfoExtTest, Task<bool>>();
            var untypedSetter = mi.GetSetter<object, object>(true);

            if (getter == null || untypedGetter == null || setter == null || untypedSetter == null) {
                getter.Should().NotBeNull();
                untypedGetter.Should().NotBeNull();
                setter.Should().NotBeNull();
                untypedSetter.Should().NotBeNull();
                throw new AssertionFailedException(
                    "We shouldn't get here - this throw is solely to suppress later nullability warnings.");
            }

            var falseTask = Task.FromResult(false);
            var trueTask = Task.FromResult(true);

            StaticTaskField = falseTask;
            StaticTaskProperty = falseTask;
            getter(null!).Should().BeSameAs(falseTask);
            untypedGetter(null!).Should().BeSameAs(falseTask);

            StaticTaskField = trueTask;
            StaticTaskProperty = trueTask;
            getter(null!).Should().BeSameAs(trueTask);
            untypedGetter(null!).Should().BeSameAs(trueTask);

            setter(null!, falseTask);
            (mi is FieldInfo ? StaticTaskField : StaticTaskProperty).Should().BeSameAs(falseTask);
            untypedSetter(null!, trueTask);
            (mi is FieldInfo ? StaticTaskField : StaticTaskProperty).Should().BeSameAs(trueTask);
        }
    }
}
