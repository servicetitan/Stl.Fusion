using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Stl.Fusion.EntityFramework.Conversion;

public class NewtonsoftJsonSerializedToStringValueConverter<T>(ConverterMappingHints? mappingHints = null)
    : ValueConverter<NewtonsoftJsonSerialized<T>, string>(
        v => v.Data,
        v => NewtonsoftJsonSerialized.New<T>(v),
        mappingHints);
