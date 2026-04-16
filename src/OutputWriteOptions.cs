namespace Bingosoft.Net.IfcDetail;

internal sealed record OutputWriteOptions(int BufferSizeBytes, bool WriteThrough)
{
    public static readonly OutputWriteOptions Default = new(4096, false);
}
