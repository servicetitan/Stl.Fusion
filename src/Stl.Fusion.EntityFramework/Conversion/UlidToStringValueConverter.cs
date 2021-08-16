using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Stl.Fusion.EntityFramework.Conversion
{
    public class UlidToStringValueConverter : ValueConverter<Ulid, string>
    {
        private static readonly ConverterMappingHints DefaultHints = new(26, unicode: false);

        public UlidToStringValueConverter(ConverterMappingHints mappingHints = null!)
            : base(x => x.ToString(), x => Ulid.Parse(x), DefaultHints.With(mappingHints))
        { }
    }
}
