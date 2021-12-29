using System.Reflection;
using FluentAssertions.Execution;
using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class MemberExtTest
{
    public Task<bool> TaskField = TaskExt.FalseTask;
    public bool BoolField;

    public Task<bool> TaskProperty { get; set; } = TaskExt.FalseTask;
    public bool BoolProperty { get; set; }

    [Fact]
    public void BoolPropertyTest()
    {
        var type = GetType();
        Test(type.GetProperty(nameof(BoolProperty))!);
        Test(type.GetField(nameof(BoolField))!);

        void Test(MemberInfo mi)
        {
            Action action = () => mi.GetGetter<object, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetGetter<object, bool>(true);
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberExtTest, bool>(true);
            action.Should().Throw<InvalidCastException>();

            var getter = mi.GetGetter<MemberExtTest, bool>();
            var untypedGetter = mi.GetGetter<object, object>(true);
            var setter = mi.GetSetter<MemberExtTest, bool>();
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
    public void TaskPropertyTest()
    {
        var type = GetType();
        var propertyName = new Symbol(nameof(TaskProperty));

        void Test(MemberInfo mi)
        {
            Action action = () => mi.GetGetter<MemberExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetGetter<MemberExtTest, Task>(true);

            _ = mi.GetGetter<MemberExtTest, object>();
            _ = mi.GetGetter<MemberExtTest, Task>();
            _ = mi.GetGetter<MemberExtTest, Task<bool>>();

            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberExtTest, string>();
            action.Should().Throw<InvalidCastException>();
            action = () => mi.GetSetter<MemberExtTest, Task>();
            action.Should().Throw<InvalidCastException>();

            _ = mi.GetSetter<MemberExtTest, Task>();
            _ = mi.GetSetter<MemberExtTest, Task<bool>>();
            _ = mi.GetSetter<MemberExtTest, object>(true);
            _ = mi.GetSetter<MemberExtTest, Task>(true);
            _ = mi.GetSetter<MemberExtTest, Task<bool>>(true);

            var getter = mi.GetGetter<MemberExtTest, Task>();
            var untypedGetter = mi.GetGetter<object, object>(true);
            var setter = mi.GetSetter<MemberExtTest, Task<bool>>();
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
}
