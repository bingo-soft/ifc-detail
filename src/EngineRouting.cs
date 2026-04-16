using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Bingosoft.Net.IfcDetail;

internal enum RequestedEngine
{
    Default,
    Baseline,
    Fast
}

internal enum EffectiveEngine
{
    Baseline,
    Fast
}

internal enum FallbackReason
{
    None,
    UnsupportedSchemaOrInput,
    ParseOrHeaderErrors,
    RuntimeExceptionFastEngine
}

internal readonly record struct EngineCounters(int Attempts, int Success, int Fallbacks);

internal sealed record ExecutionDetails(
    RequestedEngine RequestedEngine,
    EffectiveEngine EffectiveEngine,
    FallbackReason FallbackReason,
    EngineCounters Counters);

internal sealed record ProcessingResult(ExecutionDetails ExecutionDetails);

internal interface IProcessingEngine
{
    EffectiveEngine Engine { get; }

    void Process(FileInfo ifcSourceFile, FileInfo jsonTargetFile);
}

internal sealed class BaselineProcessingEngine : IProcessingEngine
{
    public EffectiveEngine Engine => EffectiveEngine.Baseline;

    public void Process(FileInfo ifcSourceFile, FileInfo jsonTargetFile)
    {
        new MaterialExtractor(jsonTargetFile).Start(ifcSourceFile);
    }
}

internal sealed class FastProcessingEngine : IProcessingEngine
{
    private static readonly Regex HeaderSchemaRegex = new("FILE_SCHEMA\\s*\\(\\s*\\(\\s*'([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public EffectiveEngine Engine => EffectiveEngine.Fast;

    public void Process(FileInfo ifcSourceFile, FileInfo jsonTargetFile)
    {
        ValidateInputForFastEngine(ifcSourceFile);
        new FastMaterialExtractor(jsonTargetFile).Start(ifcSourceFile);
    }

    private static void ValidateInputForFastEngine(FileInfo ifcSourceFile)
    {
        if (!ifcSourceFile.Extension.Equals(".ifc", StringComparison.OrdinalIgnoreCase))
        {
            throw new FastUnsupportedInputException("Fast engine supports only .ifc files.");
        }

        var header = ReadHeaderSample(ifcSourceFile, maxLines: 200, maxChars: 16 * 1024);
        if (!header.Contains("ISO-10303-21;", StringComparison.OrdinalIgnoreCase) ||
            !header.Contains("HEADER;", StringComparison.OrdinalIgnoreCase) ||
            !header.Contains("ENDSEC;", StringComparison.OrdinalIgnoreCase))
        {
            throw new FastParseHeaderException("Invalid IFC header for fast engine.");
        }

        var schemaMatch = HeaderSchemaRegex.Match(header);
        if (!schemaMatch.Success)
        {
            throw new FastParseHeaderException("FILE_SCHEMA is not found in IFC header.");
        }

        var schema = schemaMatch.Groups[1].Value.Trim();
        var isSupported = schema.StartsWith("IFC2X3", StringComparison.OrdinalIgnoreCase) ||
                          schema.StartsWith("IFC4", StringComparison.OrdinalIgnoreCase);
        if (!isSupported)
        {
            throw new FastUnsupportedInputException($"Schema '{schema}' is not supported by fast engine.");
        }
    }

    private static string ReadHeaderSample(FileInfo ifcSourceFile, int maxLines, int maxChars)
    {
        using var stream = ifcSourceFile.OpenRead();
        using var reader = new StreamReader(stream);

        var lineCount = 0;
        var charsRead = 0;
        var header = new System.Text.StringBuilder(Math.Min(maxChars, 4096));

        while (!reader.EndOfStream && lineCount < maxLines && charsRead < maxChars)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            header.AppendLine(line);
            charsRead += line.Length;
            lineCount++;

            if (line.Contains("DATA;", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return header.ToString();
    }
}

internal sealed class EngineRouter(IProcessingEngine baselineEngine, IProcessingEngine fastEngine)
{
    public ProcessingResult Process(FileInfo ifcSourceFile, FileInfo jsonTargetFile, RequestedEngine requestedEngine)
    {
        var attempts = 0;
        var success = 0;
        var fallbacks = 0;
        var fallbackReason = FallbackReason.None;

        switch (requestedEngine)
        {
            case RequestedEngine.Baseline:
                attempts++;
                baselineEngine.Process(ifcSourceFile, jsonTargetFile);
                success++;
                return BuildResult(requestedEngine, EffectiveEngine.Baseline, fallbackReason, attempts, success, fallbacks);

            case RequestedEngine.Fast:
            case RequestedEngine.Default:
                attempts++;
                try
                {
                    fastEngine.Process(ifcSourceFile, jsonTargetFile);
                    success++;
                    return BuildResult(requestedEngine, EffectiveEngine.Fast, fallbackReason, attempts, success, fallbacks);
                }
                catch (Exception ex)
                {
                    fallbackReason = ClassifyFallbackReason(ex);
                    fallbacks++;

                    attempts++;
                    baselineEngine.Process(ifcSourceFile, jsonTargetFile);
                    success++;

                    return BuildResult(requestedEngine, EffectiveEngine.Baseline, fallbackReason, attempts, success, fallbacks);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(requestedEngine), requestedEngine, "Unsupported requested engine.");
        }
    }

    private static ProcessingResult BuildResult(
        RequestedEngine requestedEngine,
        EffectiveEngine effectiveEngine,
        FallbackReason fallbackReason,
        int attempts,
        int success,
        int fallbacks)
    {
        var details = new ExecutionDetails(
            requestedEngine,
            effectiveEngine,
            fallbackReason,
            new EngineCounters(attempts, success, fallbacks));

        return new ProcessingResult(details);
    }

    private static FallbackReason ClassifyFallbackReason(Exception ex)
    {
        return ex switch
        {
            FastUnsupportedInputException => FallbackReason.UnsupportedSchemaOrInput,
            FastParseHeaderException => FallbackReason.ParseOrHeaderErrors,
            _ => FallbackReason.RuntimeExceptionFastEngine
        };
    }
}

internal sealed class FastUnsupportedInputException(string message) : Exception(message);

internal sealed class FastParseHeaderException(string message) : Exception(message);