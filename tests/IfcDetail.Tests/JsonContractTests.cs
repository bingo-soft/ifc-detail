using System.Text.Json;

using Xunit;

namespace IfcDetail.Tests;

public sealed class JsonContractTests
{
    public static IEnumerable<object[]> ScenarioNames()
        => TestDataLoader.LoadAllScenarios().Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Baseline_json_must_match_saved_snapshot(string scenarioName)
    {
        var scenario = TestDataLoader.LoadScenario(scenarioName);

        var actualBytes = JsonEngineTestHarness.BaselineCompose(
            scenario.MaterialsRawJson,
            scenario.TypesRawJson,
            scenario.PropertiesRawJson);

        var actual = JsonEngineTestHarness.Utf8(actualBytes);
        Assert.Equal(scenario.BaselineJson, actual);
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Root_must_contain_required_fields_in_strict_order(string scenarioName)
    {
        var scenario = TestDataLoader.LoadScenario(scenarioName);
        var json = JsonEngineTestHarness.Utf8(
            JsonEngineTestHarness.BaselineCompose(scenario.MaterialsRawJson, scenario.TypesRawJson, scenario.PropertiesRawJson));

        var materialsIndex = json.IndexOf("\"materials\"", StringComparison.Ordinal);
        var typesIndex = json.IndexOf("\"types\"", StringComparison.Ordinal);
        var propertiesIndex = json.IndexOf("\"properties\"", StringComparison.Ordinal);

        Assert.True(materialsIndex >= 0, "materials root field is missing");
        Assert.True(typesIndex > materialsIndex, "types must be written after materials");
        Assert.True(propertiesIndex > typesIndex, "properties must be written after types");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("materials", out _));
        Assert.True(root.TryGetProperty("types", out _));
        Assert.True(root.TryGetProperty("properties", out _));
        Assert.Equal(3, root.EnumerateObject().Count());
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Array_fields_must_be_array_and_never_null(string scenarioName)
    {
        var scenario = TestDataLoader.LoadScenario(scenarioName);
        var json = JsonEngineTestHarness.BaselineCompose(scenario.MaterialsRawJson, scenario.TypesRawJson, scenario.PropertiesRawJson);

        using var document = JsonDocument.Parse(json);
        ValidateArrayFieldRules(document.RootElement);
    }

    [Fact]
    public void Duplicate_keys_must_follow_last_occurrence_wins_semantics()
    {
        var scenario = TestDataLoader.LoadScenario("edge-cases");
        var json = JsonEngineTestHarness.BaselineCompose(scenario.MaterialsRawJson, scenario.TypesRawJson, scenario.PropertiesRawJson);

        using var document = JsonDocument.Parse(json);

        var materials = document.RootElement.GetProperty("materials");
        var dedupMaterial = materials.GetProperty("dup").GetProperty("IfcMaterial").GetProperty("Name").GetString();

        var typeTag = document.RootElement
            .GetProperty("types")
            .GetProperty("dupType")
            .GetProperty("Tag")
            .GetString();

        Assert.Equal("New", dedupMaterial);
        Assert.Equal("LatestTag", typeTag);
    }

    private static readonly HashSet<string> MaterialArrayFieldNames =
    [
        "IfcPropertySet",
        "IfcMaterialLayer",
        "IfcMaterial"
    ];

    private static void ValidateArrayFieldRules(JsonElement root)
    {
        foreach (var typeItem in root.GetProperty("types").EnumerateObject())
        {
            if (typeItem.Value.TryGetProperty("properties", out var propertiesField))
            {
                Assert.NotEqual(JsonValueKind.Null, propertiesField.ValueKind);
                Assert.Equal(JsonValueKind.Array, propertiesField.ValueKind);
            }
        }

        foreach (var propertyItem in root.GetProperty("properties").EnumerateObject())
        {
            if (propertyItem.Value.TryGetProperty("IfcPropertySingleValue", out var singleValueField))
            {
                Assert.NotEqual(JsonValueKind.Null, singleValueField.ValueKind);
                Assert.Equal(JsonValueKind.Array, singleValueField.ValueKind);
            }
        }

        foreach (var materialItem in root.GetProperty("materials").EnumerateObject())
        {
            foreach (var nestedProperty in materialItem.Value.EnumerateObject())
            {
                foreach (var candidate in nestedProperty.Value.EnumerateObject())
                {
                    if (!MaterialArrayFieldNames.Contains(candidate.Name))
                    {
                        continue;
                    }

                    Assert.NotEqual(JsonValueKind.Null, candidate.Value.ValueKind);
                    Assert.Equal(JsonValueKind.Array, candidate.Value.ValueKind);
                }
            }
        }
    }
}
