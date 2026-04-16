using System.IO;

namespace Bingosoft.Net.IfcDetail;

internal enum IntermediateStoreMode
{
    Disabled,
    MemoryMapped
}

internal sealed record MemoryScalingOptions(
    IntermediateStoreMode Mode,
    int SegmentSizeBytes,
    DirectoryInfo SpillDirectory)
{
    public static readonly MemoryScalingOptions Default = new(
        IntermediateStoreMode.Disabled,
        4 * 1024 * 1024,
        new DirectoryInfo(Path.Combine(Path.GetTempPath(), "ifc-detail-spill")));

    public bool IsEnabled => Mode != IntermediateStoreMode.Disabled;
}
