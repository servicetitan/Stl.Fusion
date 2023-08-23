using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Stl.Fusion.EntityFramework.Conversion;

public class MomentToDateTimeValueConverter(ConverterMappingHints? mappingHints = null)
    : ValueConverter<Moment, DateTime>(
        v => v.ToDateTime(),
        v => v.DefaultKind(DateTimeKind.Utc).ToMoment(),
        mappingHints);
