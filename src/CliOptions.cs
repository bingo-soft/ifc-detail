using System;
using System.Collections.Generic;
using System.IO;

namespace Bingosoft.Net.IfcDetail;

internal enum CliVerbosity
{
    None,
    Timing,
    Detailed
}

internal enum CliProgress
{
    Completed,
    Remaining,
    None
}

internal sealed record CliOptions(
    FileInfo IfcSourceFile,
    FileInfo JsonTargetFile,
    RequestedEngine RequestedEngine,
    CliVerbosity Verbosity,
    CliProgress Progress,
    OutputWriteOptions OutputWriteOptions,
    MemoryScalingOptions MemoryScalingOptions,
    bool IsHelpRequested)
{
    public static bool TryParse(string[] args, out CliOptions options, out string error)
    {
        options = null;
        error = string.Empty;

        var positional = new List<string>(2);
        var requestedEngine = RequestedEngine.Default;
        var verbosity = CliVerbosity.Detailed;
        var progress = CliProgress.None;
        var outputBufferBytes = OutputWriteOptions.Default.BufferSizeBytes;
        var writeThrough = OutputWriteOptions.Default.WriteThrough;
        var intermediateStoreMode = MemoryScalingOptions.Default.Mode;
        var intermediateSegmentBytes = MemoryScalingOptions.Default.SegmentSizeBytes;
        var spillDirectory = MemoryScalingOptions.Default.SpillDirectory;
        var isIntermediateStoreSpecified = false;
        var helpRequested = false;

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (IsHelpOption(argument))
            {
                helpRequested = true;
                continue;
            }

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(argument);
                continue;
            }

            if (IsNamedOption(argument, "--engine"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (!TryParseRequestedEngine(value, out requestedEngine))
                {
                    error = $"Unsupported engine '{value}'. Use baseline|fast.";
                    return false;
                }

                continue;
            }

            if (IsNamedOption(argument, "--verbosity"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (!TryParseVerbosity(value, out verbosity))
                {
                    error = $"Unsupported verbosity '{value}'. Use none|timing|detailed.";
                    return false;
                }

                continue;
            }

            if (IsNamedOption(argument, "--progress"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (!TryParseProgress(value, out progress))
                {
                    error = $"Unsupported progress mode '{value}'. Use completed|remaining|none.";
                    return false;
                }

                continue;
            }

            if (IsNamedOption(argument, "--output-buffer-kb"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (!int.TryParse(value, out var parsedKilobytes) || parsedKilobytes <= 0)
                {
                    error = $"Unsupported output buffer value '{value}'. Use positive integer in KB.";
                    return false;
                }

                try
                {
                    outputBufferBytes = checked(parsedKilobytes * 1024);
                }
                catch (OverflowException)
                {
                    error = $"Output buffer value '{value}' is too large.";
                    return false;
                }

                continue;
            }

            if (IsNamedOption(argument, "--write-through"))
            {
                if (TryExtractInlineOptionValue(argument, out var inlineValue))
                {
                    if (!TryParseBoolean(inlineValue, out writeThrough))
                    {
                        error = $"Unsupported write-through value '{inlineValue}'. Use true|false.";
                        return false;
                    }

                    continue;
                }

                if (TryReadOptionalBooleanValue(args, i, out var optionalBooleanValue))
                {
                    writeThrough = optionalBooleanValue;
                    i++;
                    continue;
                }

                writeThrough = true;
                continue;
            }

            if (IsNamedOption(argument, "--intermediate-store"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (!TryParseIntermediateStoreMode(value, out intermediateStoreMode))
                {
                    error = $"Unsupported intermediate store mode '{value}'. Use none|mmf.";
                    return false;
                }

                isIntermediateStoreSpecified = true;
                continue;
            }

            if (IsNamedOption(argument, "--segment-size-kb"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (!int.TryParse(value, out var parsedKilobytes) || parsedKilobytes <= 0)
                {
                    error = $"Unsupported segment size value '{value}'. Use positive integer in KB.";
                    return false;
                }

                try
                {
                    intermediateSegmentBytes = checked(parsedKilobytes * 1024);
                }
                catch (OverflowException)
                {
                    error = $"Segment size value '{value}' is too large.";
                    return false;
                }

                continue;
            }

            if (IsNamedOption(argument, "--spill-dir"))
            {
                if (!TryReadRequiredOptionValue(argument, args, ref i, out var value, out error))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "Spill directory path is empty.";
                    return false;
                }

                spillDirectory = new DirectoryInfo(value);
                continue;
            }

            error = $"Unknown option '{argument}'.";
            return false;
        }

        if (verbosity == CliVerbosity.None && progress != CliProgress.None)
        {
            error = "--progress completed|remaining is not allowed with --verbosity none.";
            return false;
        }

        if (!isIntermediateStoreSpecified)
        {
            intermediateStoreMode = GetDefaultIntermediateStoreMode(requestedEngine);
        }

        if (intermediateStoreMode == IntermediateStoreMode.Disabled &&
            (!string.Equals(spillDirectory.FullName, MemoryScalingOptions.Default.SpillDirectory.FullName, StringComparison.OrdinalIgnoreCase) ||
             intermediateSegmentBytes != MemoryScalingOptions.Default.SegmentSizeBytes))
        {
            error = "--segment-size-kb and --spill-dir require --intermediate-store mmf.";
            return false;
        }

        if (helpRequested)
        {
            options = new CliOptions(
                new FileInfo("."),
                new FileInfo("."),
                requestedEngine,
                verbosity,
                progress,
                new OutputWriteOptions(outputBufferBytes, writeThrough),
                new MemoryScalingOptions(intermediateStoreMode, intermediateSegmentBytes, spillDirectory),
                true);

            return true;
        }

        if (positional.Count == 0)
        {
            error = "IFC source path is missing.";
            return false;
        }

        if (positional.Count > 2)
        {
            error = "Too many positional arguments. Usage: ifc_metadata <source.ifc> [target.json] [options]";
            return false;
        }

        var ifcSourceFile = new FileInfo(positional[0]);
        var jsonTargetFile = positional.Count < 2
            ? new FileInfo(Path.ChangeExtension(ifcSourceFile.FullName, ".json"))
            : new FileInfo(positional[1]);

        options = new CliOptions(
            ifcSourceFile,
            jsonTargetFile,
            requestedEngine,
            verbosity,
            progress,
            new OutputWriteOptions(outputBufferBytes, writeThrough),
            new MemoryScalingOptions(intermediateStoreMode, intermediateSegmentBytes, spillDirectory),
            false);

        return true;
    }

    private static bool IsHelpOption(string argument)
    {
        return argument.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               argument.Equals("-h", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNamedOption(string argument, string optionName)
    {
        return argument.Equals(optionName, StringComparison.Ordinal) ||
               argument.StartsWith(optionName + "=", StringComparison.Ordinal);
    }

    private static bool TryReadRequiredOptionValue(string argument, string[] args, ref int index, out string value, out string error)
    {
        error = string.Empty;

        if (TryExtractInlineOptionValue(argument, out value))
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"Value is not specified for option '{argument}'.";
                return false;
            }

            return true;
        }

        var nextIndex = index + 1;
        if (nextIndex >= args.Length || args[nextIndex].StartsWith("--", StringComparison.Ordinal))
        {
            value = string.Empty;
            error = $"Value is not specified for option '{argument}'.";
            return false;
        }

        index = nextIndex;
        value = args[nextIndex];

        return true;
    }

    private static bool TryExtractInlineOptionValue(string argument, out string value)
    {
        var separatorIndex = argument.IndexOf('=');
        if (separatorIndex < 0)
        {
            value = string.Empty;
            return false;
        }

        value = argument.Substring(separatorIndex + 1);
        return true;
    }

    private static bool TryParseRequestedEngine(string value, out RequestedEngine requestedEngine)
    {
        requestedEngine = value switch
        {
            _ when value.Equals("baseline", StringComparison.OrdinalIgnoreCase) => RequestedEngine.Baseline,
            _ when value.Equals("fast", StringComparison.OrdinalIgnoreCase) => RequestedEngine.Fast,
            _ => RequestedEngine.Default
        };

        return requestedEngine != RequestedEngine.Default;
    }

    private static bool TryParseVerbosity(string value, out CliVerbosity verbosity)
    {
        verbosity = value switch
        {
            _ when value.Equals("none", StringComparison.OrdinalIgnoreCase) => CliVerbosity.None,
            _ when value.Equals("timing", StringComparison.OrdinalIgnoreCase) => CliVerbosity.Timing,
            _ when value.Equals("detailed", StringComparison.OrdinalIgnoreCase) => CliVerbosity.Detailed,
            _ => CliVerbosity.None
        };

        return value.Equals("none", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("timing", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("detailed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseProgress(string value, out CliProgress progress)
    {
        progress = value switch
        {
            _ when value.Equals("completed", StringComparison.OrdinalIgnoreCase) => CliProgress.Completed,
            _ when value.Equals("remaining", StringComparison.OrdinalIgnoreCase) => CliProgress.Remaining,
            _ => CliProgress.None
        };

        return value.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("remaining", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseIntermediateStoreMode(string value, out IntermediateStoreMode mode)
    {
        mode = value switch
        {
            _ when value.Equals("none", StringComparison.OrdinalIgnoreCase) => IntermediateStoreMode.Disabled,
            _ when value.Equals("mmf", StringComparison.OrdinalIgnoreCase) => IntermediateStoreMode.MemoryMapped,
            _ => IntermediateStoreMode.Disabled
        };

        return value.Equals("none", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("mmf", StringComparison.OrdinalIgnoreCase);
    }

    private static IntermediateStoreMode GetDefaultIntermediateStoreMode(RequestedEngine requestedEngine)
    {
        return requestedEngine switch
        {
            _ => IntermediateStoreMode.Disabled
        };
    }

    private static bool TryReadOptionalBooleanValue(string[] args, int index, out bool value)
    {
        value = false;

        var nextIndex = index + 1;
        if (nextIndex >= args.Length || args[nextIndex].StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        return TryParseBoolean(args[nextIndex], out value);
    }

    private static bool TryParseBoolean(string value, out bool parsed)
    {
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            parsed = true;
            return true;
        }

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            parsed = false;
            return true;
        }

        parsed = false;
        return false;
    }
}
