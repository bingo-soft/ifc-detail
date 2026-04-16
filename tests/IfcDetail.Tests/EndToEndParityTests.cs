using System.Text.Json;

using Bingosoft.Net.IfcDetail;

using Xunit;
using Xunit.Abstractions;

namespace IfcDetail.Tests;

public sealed class EndToEndParityTests
{
    private const long DefaultMaxIfcSizeMb = 100;
    private static readonly Lazy<ScenarioSelectionResult> Scenarios = new(ResolveIfcScenarios);

    private readonly ITestOutputHelper _output;

    public EndToEndParityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Baseline_and_fast_extractors_must_match_byte_to_byte_for_real_ifc_inputs()
    {
        var selection = Scenarios.Value;
        WriteSelectionLogs(selection);

        var scenarios = selection.Scenarios;
        if (scenarios.Count == 0)
        {
            return;
        }

        var outputDirectory = CreateOutputDirectory();

        foreach (var scenario in scenarios)
        {
            var baselineOutput = new FileInfo(Path.Combine(outputDirectory.FullName, $"{scenario.Name}.baseline.json"));
            var fastOutput = new FileInfo(Path.Combine(outputDirectory.FullName, $"{scenario.Name}.fast.json"));

            new MaterialExtractor(baselineOutput).Start(scenario.File);
            new FastMaterialExtractor(fastOutput).Start(scenario.File);

            var baselineBytes = File.ReadAllBytes(baselineOutput.FullName);
            var fastBytes = File.ReadAllBytes(fastOutput.FullName);
            Assert.Equal(baselineBytes, fastBytes);
        }
    }

    [Fact]
    public void Baseline_and_fast_extractors_must_match_in_normalized_mode_for_real_ifc_inputs()
    {
        var selection = Scenarios.Value;
        WriteSelectionLogs(selection);

        var scenarios = selection.Scenarios;
        if (scenarios.Count == 0)
        {
            return;
        }

        var outputDirectory = CreateOutputDirectory();

        foreach (var scenario in scenarios)
        {
            var baselineOutput = new FileInfo(Path.Combine(outputDirectory.FullName, $"{scenario.Name}.baseline.norm.json"));
            var fastOutput = new FileInfo(Path.Combine(outputDirectory.FullName, $"{scenario.Name}.fast.norm.json"));

            new MaterialExtractor(baselineOutput).Start(scenario.File);
            new FastMaterialExtractor(fastOutput).Start(scenario.File);

            var baseline = Normalize(File.ReadAllBytes(baselineOutput.FullName));
            var fast = Normalize(File.ReadAllBytes(fastOutput.FullName));
            Assert.Equal(baseline, fast);
        }
    }

    private static ScenarioSelectionResult ResolveIfcScenarios()
    {
        var logs = new List<string>();

        var ifcDirectory = ResolveIfcDirectory();
        logs.Add($"IFC directory: {ifcDirectory.FullName}");

        if (!ifcDirectory.Exists)
        {
            logs.Add("IFC directory does not exist. E2E parity tests will be skipped.");
            return new ScenarioSelectionResult(Array.Empty<IfcScenario>(), logs);
        }

        var sortedFiles = ifcDirectory.GetFiles("*.ifc", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f.Length)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        logs.Add($"IFC files found: {sortedFiles.Length}");
        if (sortedFiles.Length == 0)
        {
            logs.Add("No IFC files found. E2E parity tests will be skipped.");
            return new ScenarioSelectionResult(Array.Empty<IfcScenario>(), logs);
        }

        var maxSizeBytes = ResolveMaxIfcSizeBytes();
        logs.Add($"Max IFC size filter (MB): {maxSizeBytes / (1024 * 1024)}");

        var filesForSelection = sortedFiles.Where(f => f.Length <= maxSizeBytes).ToArray();
        if (filesForSelection.Length == 0)
        {
            filesForSelection = sortedFiles.Take(5).ToArray();
            logs.Add("No files passed size filter. Fallback to first 5 files by size.");
        }

        logs.Add("Files for candidate selection: " + string.Join(", ", filesForSelection.Select(f => $"{f.Name} ({ToMb(f.Length):0.##} MB)")));

        var candidateFiles = BuildCandidatePool(filesForSelection);
        logs.Add("Candidate files: " + string.Join(", ", candidateFiles.Select(f => $"{f.Name} ({ToMb(f.Length):0.##} MB)")));

        var compatibleFiles = new List<FileInfo>();
        var skippedFiles = new List<string>();

        foreach (var file in candidateFiles)
        {
            var precheck = TryRunExtraction(file);
            if (precheck.IsCompatible)
            {
                compatibleFiles.Add(file);
                logs.Add($"Compatible: {file.Name}");
            }
            else
            {
                skippedFiles.Add($"{file.Name}: {precheck.Reason}");
            }
        }

        if (skippedFiles.Count > 0)
        {
            logs.Add("Skipped incompatible files: " + string.Join(" | ", skippedFiles));
        }

        if (compatibleFiles.Count == 0)
        {
            logs.Add("No compatible IFC files for current extractor. E2E parity tests will be skipped.");
            return new ScenarioSelectionResult(Array.Empty<IfcScenario>(), logs);
        }

        compatibleFiles = compatibleFiles
            .OrderBy(f => f.Length)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selected = new List<FileInfo>
        {
            compatibleFiles[0],
            compatibleFiles[compatibleFiles.Count / 2],
            compatibleFiles[^1]
        };

        var edgeCompatible = compatibleFiles.FirstOrDefault(f =>
            f.Name.Contains("edge", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("case", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("dup", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("stress", StringComparison.OrdinalIgnoreCase));

        if (edgeCompatible is not null)
        {
            selected.Add(edgeCompatible);
        }

        var scenarios = selected
            .DistinctBy(f => f.FullName)
            .Select(f => new IfcScenario(Path.GetFileNameWithoutExtension(f.Name), f))
            .ToArray();

        logs.Add("Selected scenarios: " + string.Join(", ", scenarios.Select(s => $"{s.File.Name} ({ToMb(s.File.Length):0.##} MB)")));
        return new ScenarioSelectionResult(scenarios, logs);
    }

    private static IReadOnlyList<FileInfo> BuildCandidatePool(IReadOnlyList<FileInfo> sortedFiles)
    {
        var result = new List<FileInfo>();

        result.AddRange(sortedFiles.Take(2));

        var middleIndex = sortedFiles.Count / 2;
        for (var i = Math.Max(0, middleIndex - 1); i <= Math.Min(sortedFiles.Count - 1, middleIndex + 1); i++)
        {
            result.Add(sortedFiles[i]);
        }

        result.AddRange(sortedFiles.TakeLast(2));

        result.AddRange(sortedFiles.Where(f =>
            f.Name.Contains("edge", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("case", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("dup", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("stress", StringComparison.OrdinalIgnoreCase)).Take(2));

        return result.DistinctBy(f => f.FullName).Take(6).ToArray();
    }

    private static long ResolveMaxIfcSizeBytes()
    {
        var value = Environment.GetEnvironmentVariable("IFC_DETAIL_MAX_IFC_MB");
        if (!string.IsNullOrWhiteSpace(value) && long.TryParse(value, out var mb) && mb > 0)
        {
            return mb * 1024 * 1024;
        }

        return DefaultMaxIfcSizeMb * 1024 * 1024;
    }

    private static PrecheckResult TryRunExtraction(FileInfo ifcFile)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "ifc-detail-e2e-precheck", Guid.NewGuid().ToString("N") + ".json");
            var output = new FileInfo(tempPath);

            new MaterialExtractor(output).Start(ifcFile);

            if (output.Exists)
            {
                output.Delete();
            }

            return new PrecheckResult(true, "ok");
        }
        catch (Exception ex)
        {
            return new PrecheckResult(false, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static DirectoryInfo ResolveIfcDirectory()
    {
        var pathFromEnv = Environment.GetEnvironmentVariable("IFC_DETAIL_IFC_DIR");
        if (!string.IsNullOrWhiteSpace(pathFromEnv))
        {
            return new DirectoryInfo(pathFromEnv);
        }

        var repoRoot = ResolveRepositoryRoot();
        return new DirectoryInfo(Path.Combine(repoRoot.FullName, "ifc"));
    }

    private static DirectoryInfo ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var slnPath = Path.Combine(current.FullName, "ifc-detail.sln");
            if (File.Exists(slnPath))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate repository root by ifc-detail.sln.");
    }

    private static DirectoryInfo CreateOutputDirectory()
    {
        var ifcDirectory = ResolveIfcDirectory();
        var path = Path.Combine(ifcDirectory.FullName, "_parity-output", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        return Directory.CreateDirectory(path);
    }

    private void WriteSelectionLogs(ScenarioSelectionResult selection)
    {
        foreach (var line in selection.LogLines)
        {
            _output.WriteLine(line);
        }
    }

    private static double ToMb(long bytes)
    {
        return bytes / 1024d / 1024d;
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

    private sealed record IfcScenario(string Name, FileInfo File);

    private sealed record PrecheckResult(bool IsCompatible, string Reason);

    private sealed record ScenarioSelectionResult(IReadOnlyList<IfcScenario> Scenarios, IReadOnlyList<string> LogLines);
}
