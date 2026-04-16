using System;

namespace Bingosoft.Net.IfcDetail;

internal class Program
{
    private static readonly EngineRouter Router = new(new BaselineProcessingEngine(), new FastProcessingEngine());

    static int Main(string[] args)
    {
        if (!CliOptions.TryParse(args, out var options, out var parseError))
        {
            Console.WriteLine(parseError);
            PrintUsage();
            return 1;
        }

        if (!options.IfcSourceFile.Exists)
        {
            Console.WriteLine($"File: {options.IfcSourceFile} does not exist.");
            return 1;
        }

        try
        {
            var result = Router.Process(options.IfcSourceFile, options.JsonTargetFile, options.RequestedEngine);
            PrintExecutionDetails(result.ExecutionDetails);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: ifc_metadata <source.ifc> [target.json] [--engine baseline|fast]");
        Console.WriteLine("Default engine policy: fast with fallback to baseline.");
    }

    private static void PrintExecutionDetails(ExecutionDetails details)
    {
        Console.WriteLine($"Requested engine: {details.RequestedEngine}");
        Console.WriteLine($"Effective engine: {details.EffectiveEngine}");
        Console.WriteLine($"Fallback reason: {details.FallbackReason}");
        Console.WriteLine($"Counters: attempts={details.Counters.Attempts}, success={details.Counters.Success}, fallbacks={details.Counters.Fallbacks}");
    }
}

