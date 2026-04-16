using System.Text.Json;

using Bingosoft.Net.IfcDetail;

using Xunit;

namespace IfcDetail.Tests;

public sealed class DirectJsonEmitterTests
{
    [Fact]
    public void Preserve_order_mode_must_apply_last_occurrence_wins_without_reordering_keys()
    {
        var ifcFile = CreateTempIfcFile();
        var output = CreateTempJsonFile();

        var model = new FastIfcStepParser().Parse(ifcFile);
        var composer = new FastIfcJsonComposer(JsonEmissionMode.PreserveOrder, enforceLastOccurrenceWins: true);

        composer.Write(output, model);

        using var document = JsonDocument.Parse(File.ReadAllBytes(output.FullName));

        var types = document.RootElement.GetProperty("types");
        Assert.Equal(new[] { "keep", "dup" }, types.EnumerateObject().Select(x => x.Name).ToArray());
        Assert.Equal("NewTag", types.GetProperty("dup").GetProperty("Tag").GetString());

        var materials = document.RootElement.GetProperty("materials");
        Assert.Equal(1, materials.EnumerateObject().Count(x => x.Name == "PIPE-1"));
    }

    [Fact]
    public void Deterministic_mode_must_apply_last_occurrence_wins_and_sort_keys()
    {
        var ifcFile = CreateTempIfcFile();
        var output = CreateTempJsonFile();

        var model = new FastIfcStepParser().Parse(ifcFile);
        var composer = new FastIfcJsonComposer(JsonEmissionMode.Deterministic, enforceLastOccurrenceWins: true);

        composer.Write(output, model);

        using var document = JsonDocument.Parse(File.ReadAllBytes(output.FullName));

        var types = document.RootElement.GetProperty("types");
        Assert.Equal(new[] { "dup", "keep" }, types.EnumerateObject().Select(x => x.Name).ToArray());
        Assert.Equal("NewTag", types.GetProperty("dup").GetProperty("Tag").GetString());

        var materials = document.RootElement.GetProperty("materials");
        Assert.Equal(1, materials.EnumerateObject().Count(x => x.Name == "PIPE-1"));
    }

    private static FileInfo CreateTempIfcFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "ifc-detail-direct-emitter", Guid.NewGuid().ToString("N") + ".ifc");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var lines = new[]
        {
            "ISO-10303-21;",
            "HEADER;",
            "FILE_DESCRIPTION(('ViewDefinition [CoordinationView]'),'2;1');",
            "FILE_NAME('generated.ifc','2026-01-01T00:00:00',('author'),('org'),'preproc','originating','auth');",
            "FILE_SCHEMA(('IFC4'));",
            "ENDSEC;",
            "DATA;",
            "#1=IFCPROPERTYSET('PSET-1',$,'Pset_1',$,());",
            "#2=IFCWALLTYPE('dup',$,'WallOld',$,$,(#1),$,'OldTag',$,.STANDARD.);",
            "#3=IFCWALLTYPE('keep',$,'WallKeep',$,$,(#1),$,'KeepTag',$,.STANDARD.);",
            "#4=IFCWALLTYPE('dup',$,'WallNew',$,$,(#1),$,'NewTag',$,.STANDARD.);",
            "#5=IFCPIPESEGMENTTYPE('PIPE-1',$,'Pipe',$,$,(#1),$,'PTag',$,.NOTDEFINED.);",
            "ENDSEC;",
            "END-ISO-10303-21;"
        };

        File.WriteAllText(path, string.Join(Environment.NewLine, lines));
        return new FileInfo(path);
    }

    private static FileInfo CreateTempJsonFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "ifc-detail-direct-emitter", Guid.NewGuid().ToString("N") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return new FileInfo(path);
    }
}
