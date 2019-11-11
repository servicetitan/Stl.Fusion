using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Common;
using FluentAssertions.Execution;
using Stl.Async;
using Stl.Reflection;
using Xunit;

namespace Stl.Tests.Reflection
{
    public class PropertyExTest
    {
        public Task TaskProperty { get; set; } = Task.CompletedTask;
        public bool BoolProperty { get; set; }
        
        [Fact]
        public void FindPropertiesTest()
        {
            var type = GetType();
            var boolPropertyName = new Symbol(nameof(BoolProperty));
            var taskPropertyName = new Symbol(nameof(TaskProperty));

            type.FindProperties(_ => true, 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToArray().Should().BeEquivalentTo(new [] {boolPropertyName, taskPropertyName});
            type.FindProperties(p => p.DeclaringType == type)
                .ToArray().Should().BeEquivalentTo(new [] {boolPropertyName, taskPropertyName});
            type.FindProperties(p => typeof(Task).IsAssignableFrom(p.PropertyType))
                .ToArray().Should().BeEquivalentTo(new [] {taskPropertyName});
        }

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

            if (getter == null || untypedGetter == null || setter == null || untypedSetter == null) {
                getter.Should().NotBeNull();
                untypedGetter.Should().NotBeNull();
                setter.Should().NotBeNull();
                untypedSetter.Should().NotBeNull();
                throw new AssertionFailedException(
                    "We shouldn't get here - this throw is solely to suppress later nullability warnings.");
            }

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

            PropertyEx.Set(this, propertyName, false);
            PropertyEx.Get<bool>(this, propertyName).Should().BeFalse();
            PropertyEx.SetUntyped(this, propertyName, true);
            PropertyEx.GetUntyped(this, propertyName).Should().Be(true);
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
