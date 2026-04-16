using Bingosoft.Net.IfcDetail;

using Xunit;

namespace IfcDetail.Tests;

public sealed class CliOptionsTests
{
    [Fact]
    public void Positional_args_must_remain_supported_with_defaults()
    {
        var ok = CliOptions.TryParse(["sample.ifc"], out var options, out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrEmpty(error));
        Assert.NotNull(options);
        Assert.Equal(RequestedEngine.Default, options.RequestedEngine);
        Assert.Equal(CliVerbosity.Detailed, options.Verbosity);
        Assert.Equal(CliProgress.None, options.Progress);
        Assert.False(options.IsHelpRequested);
        Assert.Equal("sample.json", options.JsonTargetFile.Name);
    }

    [Fact]
    public void Flags_must_parse_and_map_to_cli_options()
    {
        var ok = CliOptions.TryParse(
            ["source.ifc", "target.json", "--engine", "baseline", "--verbosity=timing", "--progress", "remaining", "--output-buffer-kb", "64", "--write-through"],
            out var options,
            out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrEmpty(error));
        Assert.NotNull(options);
        Assert.Equal(RequestedEngine.Baseline, options.RequestedEngine);
        Assert.Equal(CliVerbosity.Timing, options.Verbosity);
        Assert.Equal(CliProgress.Remaining, options.Progress);
        Assert.Equal(64 * 1024, options.OutputWriteOptions.BufferSizeBytes);
        Assert.True(options.OutputWriteOptions.WriteThrough);
    }

    [Fact]
    public void Help_must_work_without_required_positional_arguments()
    {
        var ok = CliOptions.TryParse(["--help"], out var options, out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrEmpty(error));
        Assert.NotNull(options);
        Assert.True(options.IsHelpRequested);
    }

    [Fact]
    public void Verbosity_none_must_reject_progress_modes_except_none()
    {
        var ok = CliOptions.TryParse(["source.ifc", "--verbosity", "none", "--progress", "completed"], out _, out var error);

        Assert.False(ok);
        Assert.Contains("--progress completed|remaining is not allowed with --verbosity none", error);
    }

    [Fact]
    public void Verbosity_none_must_allow_progress_none()
    {
        var ok = CliOptions.TryParse(["source.ifc", "--verbosity", "none", "--progress", "none"], out var options, out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrEmpty(error));
        Assert.NotNull(options);
        Assert.Equal(CliVerbosity.None, options.Verbosity);
        Assert.Equal(CliProgress.None, options.Progress);
    }

    [Fact]
    public void Write_through_must_accept_explicit_false_value()
    {
        var ok = CliOptions.TryParse(["source.ifc", "--write-through=false"], out var options, out var error);

        Assert.True(ok);
        Assert.True(string.IsNullOrEmpty(error));
        Assert.NotNull(options);
        Assert.False(options.OutputWriteOptions.WriteThrough);
    }

    [Fact]
    public void Output_buffer_must_be_positive_integer()
    {
        var ok = CliOptions.TryParse(["source.ifc", "--output-buffer-kb", "0"], out _, out var error);

        Assert.False(ok);
        Assert.Contains("positive integer", error);
    }
}
