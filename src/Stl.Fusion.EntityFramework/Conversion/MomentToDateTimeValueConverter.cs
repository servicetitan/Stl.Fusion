using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Conversion
{
    public class MomentToDateTimeValueConverter : ValueConverter<Moment, DateTime>
    {
        public MomentToDateTimeValueConverter(ConverterMappingHints? mappingHints = null)
            : base(
                v => v.ToDateTime(),
                v => v.DefaultKind(DateTimeKind.Utc).ToMoment(),
                mappingHints)
        { }
    }
}
