using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Desktop.Models.Updates;

public sealed class UpdateManifestNotesJsonConverter : JsonConverter<IReadOnlyList<string>>
{
    public override IReadOnlyList<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return [];
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var note = reader.GetString();
            return string.IsNullOrWhiteSpace(note) ? [] : [note];
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Update manifest notes must be a string or an array of strings.");
        }

        var notes = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return notes;
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                continue;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Update manifest notes entries must be strings.");
            }

            var note = reader.GetString();
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }

        throw new JsonException("Update manifest notes array was not closed.");
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var note in value)
        {
            writer.WriteStringValue(note);
        }

        writer.WriteEndArray();
    }
}
