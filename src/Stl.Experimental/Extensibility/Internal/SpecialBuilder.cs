namespace Stl.Extensibility.Internal
{
    public readonly struct SpecialBuilder<TService>
        where TService : class
    {
        private readonly TService _service;

        public SpecialBuilder(TService service) 
            => _service = service;

        public Special<TService, TFor> For<TFor>()
            => new Special<TService, TFor>(_service);
    }
}
