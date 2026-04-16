using System.IO;

namespace Bingosoft.Net.IfcDetail;

internal sealed class FastMaterialExtractor(FileInfo jsonTargetFile)
{
    private static readonly FastIfcStepParser Parser = new();
    private static readonly FastIfcJsonComposer Composer = new();

    public void Start(FileInfo ifcFileInfo)
    {
        var model = Parser.Parse(ifcFileInfo);
        Composer.Write(jsonTargetFile, model);
    }
}
