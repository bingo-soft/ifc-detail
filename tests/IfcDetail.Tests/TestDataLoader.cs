namespace IfcDetail.Tests;

internal sealed class TestScenarioData
{
    public required string Name { get; init; }
    public required string MaterialsRawJson { get; init; }
    public required string TypesRawJson { get; init; }
    public required string PropertiesRawJson { get; init; }
    public required string BaselineJson { get; init; }
}

internal static class TestDataLoader
{
    public static IReadOnlyList<TestScenarioData> LoadAllScenarios()
    {
        var root = LocateTestDataRoot();
        return Directory
            .GetDirectories(Path.Combine(root, "inputs"))
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => LoadScenario(root, name!))
            .ToArray();
    }

    public static TestScenarioData LoadScenario(string name)
    {
        var root = LocateTestDataRoot();
        return LoadScenario(root, name);
    }

    private static TestScenarioData LoadScenario(string root, string name)
    {
        var inputPath = Path.Combine(root, "inputs", name);
        var baselinePath = Path.Combine(root, "baseline", $"{name}.json");

        return new TestScenarioData
        {
            Name = name,
            MaterialsRawJson = File.ReadAllText(Path.Combine(inputPath, "materials.json")),
            TypesRawJson = File.ReadAllText(Path.Combine(inputPath, "types.json")),
            PropertiesRawJson = File.ReadAllText(Path.Combine(inputPath, "properties.json")),
            BaselineJson = File.ReadAllText(baselinePath)
        };
    }

    private static string LocateTestDataRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = Path.Combine(current, "TestData");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException("Unable to locate TestData directory.");
    }
}
