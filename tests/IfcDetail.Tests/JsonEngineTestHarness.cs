using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IfcDetail.Tests;

internal static class JsonEngineTestHarness
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static byte[] BaselineCompose(string materialsRawJson, string typesRawJson, string propertiesRawJson)
    {
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

        writer.WriteStartObject();

        writer.WritePropertyName("materials");
        writer.WriteRawValue(materialsRawJson);

        writer.WritePropertyName("types");
        writer.WriteRawValue(typesRawJson);

        writer.WritePropertyName("properties");
        writer.WriteRawValue(propertiesRawJson);

        writer.WriteEndObject();
        writer.Flush();

        return buffer.ToArray();
    }

    public static byte[] FastCompose(string materialsRawJson, string typesRawJson, string propertiesRawJson, bool deterministic)
    {
        using var materials = JsonDocument.Parse(materialsRawJson);
        using var types = JsonDocument.Parse(typesRawJson);
        using var properties = JsonDocument.Parse(propertiesRawJson);

        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

        writer.WriteStartObject();

        if (deterministic)
        {
            WriteSection(writer, "materials", materials.RootElement);
            WriteSection(writer, "types", types.RootElement);
            WriteSection(writer, "properties", properties.RootElement);
        }
        else
        {
            WriteSection(writer, "types", types.RootElement);
            WriteSection(writer, "materials", materials.RootElement);
            WriteSection(writer, "properties", properties.RootElement);
        }

        writer.WriteEndObject();
        writer.Flush();

        return buffer.ToArray();
    }

    private static void WriteSection(Utf8JsonWriter writer, string sectionName, JsonElement jsonElement)
    {
        writer.WritePropertyName(sectionName);
        jsonElement.WriteTo(writer);
    }

    public static string NormalizeJson(byte[] jsonBytes)
    {
        using var document = JsonDocument.Parse(jsonBytes);
        var normalized = NormalizeElement(document.RootElement);
        return normalized;
    }

    private static string NormalizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => NormalizeObject(element),
            JsonValueKind.Array => NormalizeArray(element),
            _ => element.GetRawText()
        };
    }

    private static string NormalizeObject(JsonElement element)
    {
        var orderedProperties = element.EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => $"\"{property.Name}\":{NormalizeElement(property.Value)}");

        return "{" + string.Join(',', orderedProperties) + "}";
    }

    private static string NormalizeArray(JsonElement element)
    {
        var normalizedItems = element.EnumerateArray().Select(NormalizeElement);
        return "[" + string.Join(',', normalizedItems) + "]";
    }

    public static string Utf8(byte[] bytes) => Encoding.UTF8.GetString(bytes);
}
