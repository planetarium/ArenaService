namespace ArenaService.JsonConverters;

using System;
using Libplanet.Crypto;
using Newtonsoft.Json;

public class AddressJsonConverter : JsonConverter<Address>
{
    public override void WriteJson(JsonWriter writer, Address value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToHex().ToLower());
    }

    public override Address ReadJson(
        JsonReader reader,
        Type objectType,
        Address existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        string s =
            reader.Value?.ToString()
            ?? throw new JsonSerializationException("Expected a non-null Address value.");

        return new Address(s);
    }
}
