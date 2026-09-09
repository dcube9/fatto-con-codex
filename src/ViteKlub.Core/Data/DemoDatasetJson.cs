using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViteKlub.Core.Data;

public static class DemoDatasetJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static DemoDataset Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<DemoDataset>(json, SerializerOptions)
            ?? throw new JsonException("Il dataset JSON non contiene un oggetto radice valido.");
    }

    public static string Serialize(DemoDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        return JsonSerializer.Serialize(dataset, SerializerOptions);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
