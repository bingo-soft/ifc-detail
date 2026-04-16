using System;
using System.Collections.Generic;
using System.IO;

namespace Bingosoft.Net.IfcDetail;

internal sealed record CliOptions(FileInfo IfcSourceFile, FileInfo JsonTargetFile, RequestedEngine RequestedEngine)
{
    public static bool TryParse(string[] args, out CliOptions options, out string error)
    {
        options = null;
        error = string.Empty;

        if (args.Length < 1)
        {
            error = "Please specify the path to the IFC and the output json.";
            return false;
        }

        var positional = new List<string>(2);
        var requestedEngine = RequestedEngine.Default;

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(argument);
                continue;
            }

            if (argument.StartsWith("--engine", StringComparison.Ordinal))
            {
                var value = ExtractOptionValue(argument, args, ref i);
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "Engine value is not specified. Use --engine baseline|fast.";
                    return false;
                }

                if (value.Equals("baseline", StringComparison.OrdinalIgnoreCase))
                {
                    requestedEngine = RequestedEngine.Baseline;
                    continue;
                }

                if (value.Equals("fast", StringComparison.OrdinalIgnoreCase))
                {
                    requestedEngine = RequestedEngine.Fast;
                    continue;
                }

                error = $"Unsupported engine '{value}'. Use baseline|fast.";
                return false;
            }

            error = $"Unknown option '{argument}'.";
            return false;
        }

        if (positional.Count == 0)
        {
            error = "IFC source path is missing.";
            return false;
        }

        if (positional.Count > 2)
        {
            error = "Too many positional arguments. Usage: ifc_metadata <source.ifc> [target.json] [--engine baseline|fast]";
            return false;
        }

        var ifcSourceFile = new FileInfo(positional[0]);
        var jsonTargetFile = positional.Count < 2
            ? new FileInfo(Path.ChangeExtension(ifcSourceFile.FullName, ".json"))
            : new FileInfo(positional[1]);

        options = new CliOptions(ifcSourceFile, jsonTargetFile, requestedEngine);
        return true;
    }

    private static string ExtractOptionValue(string argument, string[] args, ref int index)
    {
        var separatorIndex = argument.IndexOf('=');
        if (separatorIndex >= 0)
        {
            return argument.Substring(separatorIndex + 1);
        }

        var nextIndex = index + 1;
        if (nextIndex >= args.Length)
        {
            return string.Empty;
        }

        index = nextIndex;
        return args[nextIndex];
    }
}
