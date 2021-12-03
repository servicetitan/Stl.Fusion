namespace Stl.Generators;

public sealed class TransformingGenerator<TIn, TOut> : Generator<TOut>
{
    private readonly Generator<TIn> _source;
    private readonly Func<TIn, TOut> _transformer;

    public TransformingGenerator(Generator<TIn> source, Func<TIn, TOut> transformer)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
    }

    public override TOut Next()
        => _transformer(_source.Next());
}
