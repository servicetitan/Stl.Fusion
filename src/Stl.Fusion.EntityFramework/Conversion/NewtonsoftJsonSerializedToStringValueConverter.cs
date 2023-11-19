using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Stl.Fusion.EntityFramework.Conversion;

public class NewtonsoftJsonSerializedToStringValueConverter<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    (ConverterMappingHints? mappingHints = null)
    : ValueConverter<NewtonsoftJsonSerialized<T>, string>(
#pragma warning disable IL2026
        v => v.Data,
        v => NewtonsoftJsonSerialized.New<T>(v),
#pragma warning restore IL2026
        mappingHints);
