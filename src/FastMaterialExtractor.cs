using System.IO;

namespace Bingosoft.Net.IfcDetail;

internal sealed class FastMaterialExtractor(
    FileInfo jsonTargetFile,
    JsonEmissionMode emissionMode = JsonEmissionMode.PreserveOrder,
    bool enforceLastOccurrenceWins = false,
    OutputWriteOptions? outputWriteOptions = null,
    MemoryScalingOptions? memoryScalingOptions = null)
{
    private static readonly FastIfcStepParser Parser = new();

    private readonly FastIfcJsonComposer _composer = new(emissionMode, enforceLastOccurrenceWins);

    public void Start(FileInfo ifcFileInfo)
    {
        var model = FastIfcStepParser.Parse(ifcFileInfo, memoryScalingOptions ?? MemoryScalingOptions.Default);
        _composer.Write(jsonTargetFile, model, outputWriteOptions ?? OutputWriteOptions.Default);
    }
}
