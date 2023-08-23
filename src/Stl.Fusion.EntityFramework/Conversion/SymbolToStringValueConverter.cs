using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Stl.Fusion.EntityFramework.Conversion;

public class SymbolToStringValueConverter(ConverterMappingHints? mappingHints = null)
    : ValueConverter<Symbol, string>(
        v => v.Value,
        v => new Symbol(v),
        mappingHints);
