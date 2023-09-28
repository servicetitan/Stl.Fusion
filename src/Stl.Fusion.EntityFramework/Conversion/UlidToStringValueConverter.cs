using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Stl.Fusion.EntityFramework.Conversion;

public class UlidToStringValueConverter(ConverterMappingHints mappingHints = null!)
    : ValueConverter<Ulid, string>(
        x => x.ToString()!,
        x => Ulid.Parse(x, CultureInfo.InvariantCulture),
        DefaultHints.With(mappingHints))
{
    private static readonly ConverterMappingHints DefaultHints = new(26, unicode: false);
}
