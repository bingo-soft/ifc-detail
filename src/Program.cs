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
            Console.WriteLine("Usage: ifc_metadata /path_to_file.ifc /path_to_file.json");
            Console.WriteLine("Usage: ifc_metadata /path_to_file.ifc");
            Console.WriteLine("       The file will be created in the directory of the source file, with the same name");

            Environment.Exit(1);
        }

        var ifcSourceFile = new FileInfo(args[0]);
        if (!ifcSourceFile.Exists)
        {
            Console.WriteLine($"File: {ifcSourceFile} does not exist.");
            Environment.Exit(1);
        }

        var jsonTargetFile = args.Length < 2 ? new FileInfo(Path.ChangeExtension(args[0], ".json")) : new FileInfo(args[1]);
        try
        {
            MaterialExtractor mt = new MaterialExtractor(jsonTargetFile);
            mt.Start(ifcSourceFile);

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Environment.Exit(1);
        }
    }
}
