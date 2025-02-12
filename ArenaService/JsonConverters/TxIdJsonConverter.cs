namespace ArenaService.JsonConverters;

using System;
using Libplanet.Types.Tx;
using Newtonsoft.Json;

public class TxIdJsonConverter : JsonConverter<TxId>
{
    public override void WriteJson(JsonWriter writer, TxId value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToHex().ToLower());
    }

    public override TxId ReadJson(
        JsonReader reader,
        Type objectType,
        TxId existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        string s =
            reader.Value?.ToString()
            ?? throw new JsonSerializationException("Expected a non-null TxId value.");

        return TxId.FromHex(s);
    }
}
