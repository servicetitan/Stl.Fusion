using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Reflection;
using Xunit;

namespace Stl.Tests.Reflection
{
    public class PropertyExTest
    {
        public Task TaskProperty { get; set; }
        public bool BoolProperty { get; set; }
        
        [Fact]
        public void BoolPropertyTest()
        {
            var type = GetType();
            var propertyName = new Symbol(nameof(BoolProperty));

            Action action = () => type.GetGetter<string>(propertyName);
            action.Should().Throw<InvalidCastException>();
            action = () => type.GetGetter<bool>(propertyName, true);
            action.Should().Throw<InvalidCastException>(); 
            action = () => type.GetSetter<string>(propertyName);
            action.Should().Throw<InvalidCastException>();
            action = () => type.GetSetter<bool>(propertyName, true);
            action.Should().Throw<InvalidCastException>();

            var getter = type.GetGetter<bool>(propertyName);
            var untypedGetter = type.GetGetter<object>(propertyName, true);
            var setter = type.GetSetter<bool>(propertyName);
            var untypedSetter = type.GetSetter<object>(propertyName, true);

            BoolProperty = false;
            getter.Invoke(this).Should().BeFalse();
            untypedGetter.Invoke(this).Should().Equals(false);
            
            BoolProperty = true;
            getter.Invoke(this).Should().BeTrue();
            untypedGetter.Invoke(this).Should().Equals(true);

            setter.Invoke(this, false);
            BoolProperty.Should().BeFalse();
            untypedSetter.Invoke(this, true);
            BoolProperty.Should().BeTrue();
        }
        
        [Fact]
        public void TaskPropertyTest()
        {
            var type = GetType();
            var propertyName = new Symbol(nameof(TaskProperty));

            Action action = () => type.GetGetter<string>(propertyName);
            action.Should().Throw<InvalidCastException>();
            action = () => type.GetGetter<Task>(propertyName, true);
            action.Should().Throw<InvalidCastException>(); 
            action = () => type.GetSetter<string>(propertyName);
            action.Should().Throw<InvalidCastException>();
            _ = type.GetSetter<Task>(propertyName, true); // This should work for setters

            var getter = type.GetGetter<Task>(propertyName);
            var untypedGetter = type.GetGetter<object>(propertyName, true);
            var setter = type.GetSetter<Task>(propertyName);
            var untypedSetter = type.GetSetter<object>(propertyName, true);

            var falseTask = Task.FromResult(false);
            var trueTask = Task.FromResult(true);

            TaskProperty = falseTask;
            getter.Invoke(this).Should().BeSameAs(falseTask);
            untypedGetter.Invoke(this).Should().BeSameAs(falseTask);
            
            TaskProperty = trueTask;
            getter.Invoke(this).Should().BeSameAs(trueTask);
            untypedGetter.Invoke(this).Should().BeSameAs(trueTask);

            setter.Invoke(this, falseTask);
            TaskProperty.Should().BeSameAs(falseTask);
            untypedSetter.Invoke(this, trueTask);
            TaskProperty.Should().BeSameAs(trueTask);
        }
    }
}
