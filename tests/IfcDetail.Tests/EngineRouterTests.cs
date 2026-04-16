using Bingosoft.Net.IfcDetail;

using Xunit;

namespace IfcDetail.Tests;

public sealed class EngineRouterTests
{
    [Fact]
    public void Baseline_request_must_use_baseline_without_fallback()
    {
        var baseline = new FakeEngine(EffectiveEngine.Baseline, shouldThrow: false);
        var fast = new FakeEngine(EffectiveEngine.Fast, shouldThrow: false);

        var router = new EngineRouter(baseline, fast);
        var result = router.Process(CreateSourceFile(), CreateTargetFile(), RequestedEngine.Baseline);

        Assert.Equal(1, baseline.Calls);
        Assert.Equal(0, fast.Calls);

        Assert.Equal(RequestedEngine.Baseline, result.ExecutionDetails.RequestedEngine);
        Assert.Equal(EffectiveEngine.Baseline, result.ExecutionDetails.EffectiveEngine);
        Assert.Equal(FallbackReason.None, result.ExecutionDetails.FallbackReason);
        Assert.Equal(1, result.ExecutionDetails.Counters.Attempts);
        Assert.Equal(1, result.ExecutionDetails.Counters.Success);
        Assert.Equal(0, result.ExecutionDetails.Counters.Fallbacks);
    }

    [Fact]
    public void Fast_request_must_fallback_to_baseline_when_fast_throws_runtime_exception()
    {
        var baseline = new FakeEngine(EffectiveEngine.Baseline, shouldThrow: false);
        var fast = new FakeEngine(EffectiveEngine.Fast, shouldThrow: true);

        var router = new EngineRouter(baseline, fast);
        var result = router.Process(CreateSourceFile(), CreateTargetFile(), RequestedEngine.Fast);

        Assert.Equal(1, fast.Calls);
        Assert.Equal(1, baseline.Calls);

        Assert.Equal(EffectiveEngine.Baseline, result.ExecutionDetails.EffectiveEngine);
        Assert.Equal(FallbackReason.RuntimeExceptionFastEngine, result.ExecutionDetails.FallbackReason);
        Assert.Equal(2, result.ExecutionDetails.Counters.Attempts);
        Assert.Equal(1, result.ExecutionDetails.Counters.Success);
        Assert.Equal(1, result.ExecutionDetails.Counters.Fallbacks);
    }

    [Fact]
    public void Fast_request_must_report_unsupported_schema_input_fallback_reason()
    {
        var baseline = new FakeEngine(EffectiveEngine.Baseline, shouldThrow: false);
        var fast = new FakeEngine(EffectiveEngine.Fast, shouldThrow: true, exceptionFactory: static () => new FastUnsupportedInputException("unsupported"));

        var router = new EngineRouter(baseline, fast);
        var result = router.Process(CreateSourceFile(), CreateTargetFile(), RequestedEngine.Fast);

        Assert.Equal(FallbackReason.UnsupportedSchemaOrInput, result.ExecutionDetails.FallbackReason);
    }

    [Fact]
    public void Fast_request_must_report_parse_header_fallback_reason()
    {
        var baseline = new FakeEngine(EffectiveEngine.Baseline, shouldThrow: false);
        var fast = new FakeEngine(EffectiveEngine.Fast, shouldThrow: true, exceptionFactory: static () => new FastParseHeaderException("parse"));

        var router = new EngineRouter(baseline, fast);
        var result = router.Process(CreateSourceFile(), CreateTargetFile(), RequestedEngine.Fast);

        Assert.Equal(FallbackReason.ParseOrHeaderErrors, result.ExecutionDetails.FallbackReason);
    }

    [Fact]
    public void Default_request_must_use_fast_policy()
    {
        var baseline = new FakeEngine(EffectiveEngine.Baseline, shouldThrow: false);
        var fast = new FakeEngine(EffectiveEngine.Fast, shouldThrow: false);

        var router = new EngineRouter(baseline, fast);
        var result = router.Process(CreateSourceFile(), CreateTargetFile(), RequestedEngine.Default);

        Assert.Equal(1, fast.Calls);
        Assert.Equal(0, baseline.Calls);
        Assert.Equal(EffectiveEngine.Fast, result.ExecutionDetails.EffectiveEngine);
    }

    private static FileInfo CreateSourceFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "ifc-detail-tests", Guid.NewGuid().ToString("N") + ".ifc");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "ISO-10303-21;");
        return new FileInfo(path);
    }

    private static FileInfo CreateTargetFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "ifc-detail-tests", Guid.NewGuid().ToString("N") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return new FileInfo(path);
    }

    private sealed class FakeEngine(EffectiveEngine engine, bool shouldThrow = false, Func<Exception>? exceptionFactory = null) : IProcessingEngine
    {
        public int Calls { get; private set; }

        public EffectiveEngine Engine { get; } = engine;

        public void Process(FileInfo ifcSourceFile, FileInfo jsonTargetFile, OutputWriteOptions outputWriteOptions)
        {
            Calls++;
            if (!shouldThrow)
            {
                return;
            }

            throw (exceptionFactory?.Invoke() ?? new InvalidOperationException("fast runtime failure"));
        }
    }
}
