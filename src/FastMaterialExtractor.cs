using System.IO;

namespace Bingosoft.Net.IfcDetail;

internal sealed class FastMaterialExtractor(
    FileInfo jsonTargetFile,
    JsonEmissionMode emissionMode = JsonEmissionMode.PreserveOrder,
    bool enforceLastOccurrenceWins = false)
{
    private static readonly FastIfcStepParser Parser = new();

    private readonly FastIfcJsonComposer _composer = new(emissionMode, enforceLastOccurrenceWins);

    public void Start(FileInfo ifcFileInfo)
    {
        var model = Parser.Parse(ifcFileInfo);
        _composer.Write(jsonTargetFile, model);
    }
}
