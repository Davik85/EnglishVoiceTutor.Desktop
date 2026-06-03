using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Services.Cms;

public static class CmsContentJson
{
    private static readonly JsonSerializerOptions DeterministicJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public static string SerializeDeterministic<T>(T value)
    {
        return JsonSerializer.Serialize(value, DeterministicJsonOptions);
    }

    public static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string EmptyArrayJson => "[]";
}
