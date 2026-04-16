using Xunit;

namespace IfcDetail.Tests;

public sealed class JsonParityTests
{
    public static IEnumerable<object[]> ScenarioNames()
        => TestDataLoader.LoadAllScenarios().Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Fast_engine_must_match_baseline_byte_to_byte_when_deterministic_enabled(string scenarioName)
    {
        var scenario = TestDataLoader.LoadScenario(scenarioName);

        var baseline = JsonEngineTestHarness.BaselineCompose(
            scenario.MaterialsRawJson,
            scenario.TypesRawJson,
            scenario.PropertiesRawJson);

        var fast = JsonEngineTestHarness.FastCompose(
            scenario.MaterialsRawJson,
            scenario.TypesRawJson,
            scenario.PropertiesRawJson,
            deterministic: true);

        Assert.Equal(baseline, fast);
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Fast_engine_must_match_baseline_on_normalized_json_when_deterministic_disabled(string scenarioName)
    {
        var scenario = TestDataLoader.LoadScenario(scenarioName);

        var baseline = JsonEngineTestHarness.BaselineCompose(
            scenario.MaterialsRawJson,
            scenario.TypesRawJson,
            scenario.PropertiesRawJson);

        var fast = JsonEngineTestHarness.FastCompose(
            scenario.MaterialsRawJson,
            scenario.TypesRawJson,
            scenario.PropertiesRawJson,
            deterministic: false);

        var baselineNormalized = JsonEngineTestHarness.NormalizeJson(baseline);
        var fastNormalized = JsonEngineTestHarness.NormalizeJson(fast);

        Assert.Equal(baselineNormalized, fastNormalized);
    }
}
