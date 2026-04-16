using System.Diagnostics;
using System.Text.Json;

using Bingosoft.Net.IfcDetail;

using Xunit;

namespace IfcDetail.Tests;

public sealed class FastParserPerformanceTests
{
    [Fact]
    public void Fast_parser_output_must_match_baseline_on_minimal_ifc()
    {
        var ifc = CreateIfcDataset(repeatCount: 1);
        var ifcFile = CreateTempFile("minimal", ".ifc", ifc);

        var baselineOutput = CreateTempFileInfo("baseline", ".json");
        var fastOutput = CreateTempFileInfo("fast", ".json");

        new MaterialExtractor(baselineOutput).Start(ifcFile);
        new FastMaterialExtractor(fastOutput).Start(ifcFile);

        var baselineNormalized = NormalizeJson(File.ReadAllText(baselineOutput.FullName));
        var fastNormalized = NormalizeJson(File.ReadAllText(fastOutput.FullName));

        Assert.Equal(baselineNormalized, fastNormalized);
    }

    [Fact]
    public void Fast_parser_must_decode_ifc_unicode_escape_sequences()
    {
        var ifc = CreateIfcDatasetWithUnicodeEscapes();
        var ifcFile = CreateTempFile("unicode", ".ifc", ifc);

        var baselineOutput = CreateTempFileInfo("baseline-unicode", ".json");
        var fastOutput = CreateTempFileInfo("fast-unicode", ".json");

        new MaterialExtractor(baselineOutput).Start(ifcFile);
        new FastMaterialExtractor(fastOutput).Start(ifcFile);

        var baselineJson = File.ReadAllText(baselineOutput.FullName);
        var fastJson = File.ReadAllText(fastOutput.FullName);

        Assert.Equal(NormalizeJson(baselineJson), NormalizeJson(fastJson));
        Assert.Contains("By default", fastJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\\X2\\", fastJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Fast_parser_mmf_mode_output_must_match_baseline_on_typical_dataset()
    {
        var ifc = CreateIfcDataset(repeatCount: 200);
        var ifcFile = CreateTempFile("mmf", ".ifc", ifc);

        var baselineOutput = CreateTempFileInfo("baseline-mmf", ".json");
        var fastOutput = CreateTempFileInfo("fast-mmf", ".json");
        var spillDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ifc-detail-fast-tests", "spill", Guid.NewGuid().ToString("N")));

        new MaterialExtractor(baselineOutput).Start(ifcFile);
        new FastMaterialExtractor(
            fastOutput,
            outputWriteOptions: OutputWriteOptions.Default,
            memoryScalingOptions: new MemoryScalingOptions(IntermediateStoreMode.MemoryMapped, 256 * 1024, spillDirectory)).Start(ifcFile);

        Assert.Equal(NormalizeJson(File.ReadAllText(baselineOutput.FullName)), NormalizeJson(File.ReadAllText(fastOutput.FullName)));
    }

    [Fact]
    public void Fast_parser_must_reduce_allocations_and_time_on_typical_dataset()
    {
        var ifc = CreateIfcDataset(repeatCount: 500);
        var ifcFile = CreateTempFile("perf", ".ifc", ifc);

        var baselineOutput = CreateTempFileInfo("baseline-perf", ".json");
        var fastOutput = CreateTempFileInfo("fast-perf", ".json");

        _ = Measure(() => new MaterialExtractor(baselineOutput).Start(ifcFile));
        _ = Measure(() => new FastMaterialExtractor(fastOutput).Start(ifcFile));

        var baseline = Measure(() => new MaterialExtractor(baselineOutput).Start(ifcFile));
        var fast = Measure(() => new FastMaterialExtractor(fastOutput).Start(ifcFile));

        Assert.True(fast.ElapsedMilliseconds < baseline.ElapsedMilliseconds, $"Fast path time must be lower. baseline={baseline.ElapsedMilliseconds}ms fast={fast.ElapsedMilliseconds}ms");
        Assert.True(fast.AllocatedBytes < baseline.AllocatedBytes, $"Fast path allocations must be lower. baseline={baseline.AllocatedBytes}B fast={fast.AllocatedBytes}B");
    }

    private static Measurement Measure(Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetTotalAllocatedBytes(true);
        var sw = Stopwatch.StartNew();

        action();

        sw.Stop();
        var after = GC.GetTotalAllocatedBytes(true);
        return new Measurement(sw.ElapsedMilliseconds, after - before);
    }

    private static FileInfo CreateTempFile(string prefix, string extension, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), "ifc-detail-fast-tests", $"{prefix}-{Guid.NewGuid():N}{extension}");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    private static FileInfo CreateTempFileInfo(string prefix, string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), "ifc-detail-fast-tests", $"{prefix}-{Guid.NewGuid():N}{extension}");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return new FileInfo(path);
    }

    private static string CreateIfcDatasetWithUnicodeEscapes()
    {
        var lines = new List<string>
        {
            "ISO-10303-21;",
            "HEADER;",
            "FILE_DESCRIPTION(('ViewDefinition [CoordinationView]'),'2;1');",
            "FILE_NAME('generated.ifc','2026-01-01T00:00:00',('author'),('org'),'preproc','originating','auth');",
            "FILE_SCHEMA(('IFC4'));",
            "ENDSEC;",
            "DATA;",
            "#1=IFCPROPERTYSINGLEVALUE('FireRating',$,IFCLABEL('\\X2\\00420079002000640065006600610075006C0074\\X0\\'),$);",
            "#2=IFCPROPERTYSET('PSET-UNICODE',$,'Pset_WallCommon',$,(#1));",
            "#3=IFCMATERIAL('\\X2\\00420079002000640065006600610075006C0074\\X0\\',$,$);",
            "#4=IFCMATERIALLAYER(#3,200.,$,$,$,$,$);",
            "#5=IFCMATERIALLAYERSET((#4),'LayerSet',$);",
            "#6=IFCMATERIALLAYERSETUSAGE(#5,.AXIS2.,.POSITIVE.,0.,$);",
            "#7=IFCWALLTYPE('WALL-UNICODE',$,'Wall',$,$,(#2),$,'TagUnicode',$,.STANDARD.);",
            "#8=IFCPIPESEGMENTTYPE('PIPE-UNICODE',$,'Pipe',$,$,(#2),$,'PTagUnicode',$,.NOTDEFINED.);",
            "ENDSEC;",
            "END-ISO-10303-21;"
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string CreateIfcDataset(int repeatCount)
    {
        var lines = new List<string>
        {
            "ISO-10303-21;",
            "HEADER;",
            "FILE_DESCRIPTION(('ViewDefinition [CoordinationView]'),'2;1');",
            "FILE_NAME('generated.ifc','2026-01-01T00:00:00',('author'),('org'),'preproc','originating','auth');",
            "FILE_SCHEMA(('IFC4'));",
            "ENDSEC;",
            "DATA;"
        };

        var id = 1;
        for (var i = 0; i < repeatCount; i++)
        {
            var suffix = i.ToString();
            lines.Add($"#{id}=IFCPROPERTYSINGLEVALUE('FireRating{suffix}',$,IFCLABEL('A{suffix}'),$);");
            var propertySingleValueId = id;
            id++;

            lines.Add($"#{id}=IFCPROPERTYSET('PSET-{suffix}',$,'Pset_WallCommon{suffix}',$,(#{propertySingleValueId}));");
            var propertySetId = id;
            id++;

            lines.Add($"#{id}=IFCMATERIAL('Steel{suffix}',$,$);");
            var materialId = id;
            id++;

            lines.Add($"#{id}=IFCMATERIALLAYER(#{materialId},200.,$,$,$,$,$);");
            var materialLayerId = id;
            id++;

            lines.Add($"#{id}=IFCMATERIALLAYERSET((#{materialLayerId}),'LayerSet{suffix}',$);");
            var materialLayerSetId = id;
            id++;

            lines.Add($"#{id}=IFCMATERIALLAYERSETUSAGE(#{materialLayerSetId},.AXIS2.,.POSITIVE.,0.,$);");
            id++;

            lines.Add($"#{id}=IFCWALLTYPE('WALL-{suffix}',$,'Wall{suffix}',$,$,(#{propertySetId}),$,'Tag{suffix}',$,.STANDARD.);");
            id++;

            lines.Add($"#{id}=IFCPIPESEGMENTTYPE('PIPE-{suffix}',$,'Pipe{suffix}',$,$,(#{propertySetId}),$,'PTag{suffix}',$,.NOTDEFINED.);");
            id++;
        }

        lines.Add("ENDSEC;");
        lines.Add("END-ISO-10303-21;");

        return string.Join(Environment.NewLine, lines);
    }

    private static string NormalizeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
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

    private readonly record struct Measurement(long ElapsedMilliseconds, long AllocatedBytes);
}
