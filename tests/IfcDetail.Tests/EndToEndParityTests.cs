using System.Text.Json;

using Bingosoft.Net.IfcDetail;

using Xunit;

namespace IfcDetail.Tests;

public sealed class EndToEndParityTests
{
    [Fact]
    public void Baseline_and_fast_extractors_must_match_byte_to_byte_for_real_ifc_inputs()
    {
        var scenariosRoot = CreateScenarioDirectory();
        var scenarios = IfcScenarioFactory.CreateScenarios(scenariosRoot.FullName);

        foreach (var scenario in scenarios)
        {
            var baselineOutput = new FileInfo(Path.Combine(scenariosRoot.FullName, $"{scenario.Name}.baseline.json"));
            var fastOutput = new FileInfo(Path.Combine(scenariosRoot.FullName, $"{scenario.Name}.fast.json"));

            new MaterialExtractor(baselineOutput).Start(scenario.IfcFile);
            new FastMaterialExtractor(fastOutput).Start(scenario.IfcFile);

            var baselineBytes = File.ReadAllBytes(baselineOutput.FullName);
            var fastBytes = File.ReadAllBytes(fastOutput.FullName);

            Assert.Equal(baselineBytes, fastBytes);
        }
    }

    [Fact]
    public void Baseline_and_fast_extractors_must_match_in_normalized_mode_for_real_ifc_inputs()
    {
        var scenariosRoot = CreateScenarioDirectory();
        var scenarios = IfcScenarioFactory.CreateScenarios(scenariosRoot.FullName);

        foreach (var scenario in scenarios)
        {
            var baselineOutput = new FileInfo(Path.Combine(scenariosRoot.FullName, $"{scenario.Name}.baseline.norm.json"));
            var fastOutput = new FileInfo(Path.Combine(scenariosRoot.FullName, $"{scenario.Name}.fast.norm.json"));

            new MaterialExtractor(baselineOutput).Start(scenario.IfcFile);
            new FastMaterialExtractor(fastOutput).Start(scenario.IfcFile);

            var baseline = Normalize(File.ReadAllBytes(baselineOutput.FullName));
            var fast = Normalize(File.ReadAllBytes(fastOutput.FullName));

            Assert.Equal(baseline, fast);
        }
    }

    private static DirectoryInfo CreateScenarioDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "ifc-detail-e2e", Guid.NewGuid().ToString("N"));
        return Directory.CreateDirectory(root);
    }

    private static string Normalize(byte[] payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return NormalizeElement(doc.RootElement);
    }

    private static string NormalizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => "{" + string.Join(',', element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => $"\"{x.Name}\":{NormalizeElement(x.Value)}")) + "}",
            JsonValueKind.Array => "[" + string.Join(',', element.EnumerateArray().Select(NormalizeElement)) + "]",
            _ => element.GetRawText()
        };
    }
}
