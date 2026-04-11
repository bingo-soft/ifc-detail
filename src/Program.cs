using System;
using System.IO;
using System.Threading.Tasks;

namespace Bingosoft.Net.IfcDetail;

internal class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please specify the path to the IFC and the output json.");
            Console.WriteLine("Usage: ifc_metadata /path_to_file.ifc /path_to_file.json [--profile]");
            Console.WriteLine("Usage: ifc_metadata /path_to_file.ifc [--profile]");
            Console.WriteLine("       The file will be created in the directory of the source file, with the same name");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --profile    Enable performance profiling and show detailed report");

            Environment.Exit(1);
        }

        // Parse arguments
        var enableProfiling = false;
        var ifcPath = args[0];
        string jsonPath = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--profile")
            {
                enableProfiling = true;
            }
            else if (jsonPath == null)
            {
                jsonPath = args[i];
            }
        }

        var ifcSourceFile = new FileInfo(ifcPath);
        if (!ifcSourceFile.Exists)
        {
            Console.WriteLine($"File: {ifcSourceFile} does not exist.");
            Environment.Exit(1);
        }

        var jsonTargetFile = jsonPath != null
            ? new FileInfo(jsonPath)
            : new FileInfo(Path.ChangeExtension(ifcPath, ".json"));

        try
        {
            MaterialExtractor mt = new MaterialExtractor(jsonTargetFile);
            mt.Start(ifcSourceFile, enableProfiling);

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Environment.Exit(1);
        }
    }
}
