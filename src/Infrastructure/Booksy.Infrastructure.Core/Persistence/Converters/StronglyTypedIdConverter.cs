using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

public class StronglyTypedIdConverter<TStronglyTypedId> : ValueConverter<TStronglyTypedId, Guid>
    where TStronglyTypedId : class
{
    public StronglyTypedIdConverter(
        Func<Guid, TStronglyTypedId> fromGuid,
        ConverterMappingHints mappingHints = null)
        : base(
            id => GetGuidValue(id),
            value => fromGuid(value),
            mappingHints)
    {
    }

    private static Guid GetGuidValue(TStronglyTypedId id)
    {
        // Assumes ServiceId has a .Value property
        var valueProperty = typeof(TStronglyTypedId).GetProperty("Value");
        return (Guid)valueProperty?.GetValue(id);
    }
}

public class DictionaryJsonConverter : ValueConverter<Dictionary<string, string>, string>
{
    public DictionaryJsonConverter()
        : base(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null)
                 ?? new Dictionary<string, string>())
    {
    }
}
