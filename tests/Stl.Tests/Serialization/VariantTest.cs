using System.Text.Json.Serialization;

namespace Stl.Tests.Serialization;

public class VariantTest : TestBase
{
    public abstract record Shape
    {
        public double Area { get; init; }
    }

    [DataContract]
    public record Circle : Shape
    {
        [DataMember(Order = 0)]
        public double R { get; init; }
    }

    [DataContract]
    public record Box : Shape
    {
        [DataMember(Order = 0)]
        public double Width { get; init; }
        [DataMember(Order = 1)]
        public double Height { get; init; }
    }

    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public class ShapeVariant : Variant<Shape>
    {
        [DataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Circle? Circle { get => Get<Circle>(); init => Set(value); }

        [DataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Box? Box { get => Get<Box>(); init => Set(value); }

        public ShapeVariant() { }
        [JsonConstructor, Newtonsoft.Json.JsonConstructor]
        public ShapeVariant(Circle? circle, Box? box)
            : base((Shape?) circle ?? box) { }
    }

    public VariantTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void BasicTest()
    {
        var vNull = new ShapeVariant();
        vNull.Should().Be(new ShapeVariant());

        var vc1 = new ShapeVariant() { Circle = new Circle() { R = 1 } };
        var vc2 = new ShapeVariant() { Circle = new Circle() { R = 1 } };
        vc2.Should().Be(vc2);
        vc2.Should().Be(vc1);
        vc2.Should().NotBe(vNull);

        var vb1 = new ShapeVariant() { Box = new Box() { Width = 10 } };
        var vb2 = new ShapeVariant() { Box = new Box() { Width = 10 } };
        vb2.Should().Be(vb2);
        vb2.Should().Be(vb1);
        vb2.Should().NotBe(vc1);
        vb2.Should().NotBe(vNull);

        var vb3 = new ShapeVariant() { Box = new Box() { Width = 20 } };
        vb3.Should().Be(vb3);
        vb3.Should().NotBe(vb1);
        vb3.Should().NotBe(vNull);
    }

    [Fact]
    public void SerializationTest()
    {
        var vNull1 = new ShapeVariant();
        var vNull2 = vNull1.PassThroughAllSerializers(Out);
        vNull2.Should().Be(vNull1);

        var vc1 = new ShapeVariant() { Circle = new Circle() { R = 1 } };
        var vc2 = vc1.PassThroughAllSerializers(Out);
        vc2.Should().Be(vc1);

        var vb1 = new ShapeVariant() { Box = new Box() { Width = 10 } };
        var vb2 = vb1.PassThroughAllSerializers(Out);
        vb2.Should().Be(vb1);
        vb2.Should().NotBe(vc1);
    }
}
