using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bingosoft.Net.IfcDetail;

internal class Program
{
    private const int TotalProgressSteps = 2;
    private static readonly Regex HeaderSchemaRegex = new("FILE_SCHEMA\\s*\\(\\s*\\(\\s*'([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly EngineRouter Router = new(new BaselineProcessingEngine(), new FastProcessingEngine());

    static int Main(string[] args)
    {
        if (!CliOptions.TryParse(args, out var options, out var parseError))
        {
            Console.WriteLine(parseError);
            PrintUsage();
            return 1;
        }

        if (options.IsHelpRequested)
        {
            PrintUsage();
            return 0;
        }

        if (!options.IfcSourceFile.Exists)
        {
            WriteLineUnlessSilent(options.Verbosity, $"File: {options.IfcSourceFile} does not exist.");
            return 1;
        }

        var completedSteps = 1;
        PrintProgress(options.Progress, completedSteps, "validated input arguments");

        var schema = TryReadIfcSchema(options.IfcSourceFile, out var detectedSchema)
            ? detectedSchema
            : "unknown";

        var process = Process.GetCurrentProcess();
        var peakWorkingSetBytes = process.PeakWorkingSet64;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = Router.Process(
                options.IfcSourceFile,
                options.JsonTargetFile,
                options.RequestedEngine,
                options.OutputWriteOptions,
                options.MemoryScalingOptions);

            completedSteps = 2;
            PrintProgress(options.Progress, completedSteps, "written output file");

            stopwatch.Stop();
            peakWorkingSetBytes = Math.Max(peakWorkingSetBytes, Process.GetCurrentProcess().PeakWorkingSet64);
            PrintExecutionDetails(result.ExecutionDetails, options.Verbosity, stopwatch.Elapsed, peakWorkingSetBytes, schema);
            return 0;
        }
        catch (Exception ex)
        {
            WriteLineUnlessSilent(options.Verbosity, ex.ToString());
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: ifc_metadata <source.ifc> [target.json] [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --engine baseline|fast            Force parser engine; default policy is fast with fallback to baseline.");
        Console.WriteLine("  --verbosity none|timing|detailed  none: no runtime output; timing: elapsed only; detailed: full report.");
        Console.WriteLine("  --progress completed|remaining|none");
        Console.WriteLine("  --output-buffer-kb <positive-int> Output FileStream buffer size in KB.");
        Console.WriteLine("  --write-through[=true|false]      Enables FileOptions.WriteThrough.");
        Console.WriteLine("  --intermediate-store none|mmf     Memory scaling mode; default is none.");
        Console.WriteLine("  --segment-size-kb <positive-int>  MMF read segment size in KB.");
        Console.WriteLine("  --spill-dir <path>                Intermediate spill directory for oversized entities.");
        Console.WriteLine("  --help, -h");
        Console.WriteLine("Validation:");
        Console.WriteLine("  --verbosity none requires --progress none.");
        Console.WriteLine("  --segment-size-kb and --spill-dir require --intermediate-store mmf.");
        Console.WriteLine("Positional arguments remain supported: <source.ifc> [target.json].");
    }

    private static void PrintProgress(CliProgress progressMode, int completedSteps, string activity)
    {
        switch (progressMode)
        {
            case CliProgress.Completed:
                Console.WriteLine($"Progress: completed {completedSteps}/{TotalProgressSteps} - {activity}.");
                break;
            case CliProgress.Remaining:
                Console.WriteLine($"Progress: remaining {TotalProgressSteps - completedSteps}/{TotalProgressSteps} - {activity}.");
                break;
            case CliProgress.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(progressMode), progressMode, "Unsupported progress mode.");
        }
    }

    private static void PrintExecutionDetails(ExecutionDetails details, CliVerbosity verbosity, TimeSpan elapsed, long peakWorkingSetBytes, string schema)
    {
        switch (verbosity)
        {
            case CliVerbosity.None:
                return;

            case CliVerbosity.Timing:
                Console.WriteLine($"Elapsed: {FormatElapsed(elapsed)}");
                return;

            case CliVerbosity.Detailed:
                Console.WriteLine($"Schema: {schema}");
                Console.WriteLine($"Requested parser: {details.RequestedEngine}");
                Console.WriteLine($"Effective parser: {details.EffectiveEngine}");
                Console.WriteLine($"Fallback reason: {details.FallbackReason}");
                Console.WriteLine($"Fallback count: {details.Counters.Fallbacks}");
                Console.WriteLine($"Counters: attempts={details.Counters.Attempts}, success={details.Counters.Success}, fallbacks={details.Counters.Fallbacks}");
                Console.WriteLine($"Elapsed: {FormatElapsed(elapsed)}");
                Console.WriteLine($"Peak memory: {FormatBytes(peakWorkingSetBytes)}");
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, "Unsupported verbosity.");
        }
    }

    private static void WriteLineUnlessSilent(CliVerbosity verbosity, string line)
    {
        if (verbosity != CliVerbosity.None)
        {
            Console.WriteLine(line);
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 1)
        {
            return $"{elapsed.TotalMilliseconds:0.###} ms";
        }

        if (elapsed.TotalMinutes < 1)
        {
            return $"{elapsed.TotalSeconds:0.###} s";
        }

        return elapsed.TotalHours < 1
            ? $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}"
            : $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}";
    }

    private static string FormatBytes(long bytes)
    {
        var abs = Math.Abs(bytes);
        if (abs < 1024)
        {
            return $"{bytes} B";
        }

        if (abs < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.###} KB";
        }

        if (abs < 1024L * 1024 * 1024)
        {
            return $"{bytes / (1024d * 1024):0.###} MB";
        }

        return $"{bytes / (1024d * 1024 * 1024):0.###} GB";
    }

    private static bool TryReadIfcSchema(FileInfo ifcSourceFile, out string schema)
    {
        schema = string.Empty;

        try
        {
            using var stream = ifcSourceFile.OpenRead();
            using var reader = new StreamReader(stream);

            var lineCount = 0;
            var charsRead = 0;
            var header = new StringBuilder(4 * 1024);

            while (!reader.EndOfStream && lineCount < 200 && charsRead < 16 * 1024)
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

            var match = HeaderSchemaRegex.Match(header.ToString());
            if (!match.Success)
            {
                return false;
            }

            schema = match.Groups[1].Value.Trim();
            return !string.IsNullOrWhiteSpace(schema);
        }
        catch
        {
            return false;
        }
    }
}

