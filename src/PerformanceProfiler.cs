using System;
using System.Diagnostics;

namespace Bingosoft.Net.IfcDetail;

internal sealed class PerformanceProfiler : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly long _initialMemory;
    private readonly long _initialGcCollections0;
    private readonly long _initialGcCollections1;
    private readonly long _initialGcCollections2;

    private long _peakMemory;
    private int _entitiesProcessed;

    public PerformanceProfiler()
    {
        // Force GC to get accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _initialMemory = GC.GetTotalMemory(false);
        _initialGcCollections0 = GC.CollectionCount(0);
        _initialGcCollections1 = GC.CollectionCount(1);
        _initialGcCollections2 = GC.CollectionCount(2);
        _peakMemory = _initialMemory;

        _stopwatch = Stopwatch.StartNew();
    }

    public void RecordEntity()
    {
        _entitiesProcessed++;

        // Sample memory every 100 entities to avoid overhead
        if (_entitiesProcessed % 100 == 0)
        {
            var currentMemory = GC.GetTotalMemory(false);
            if (currentMemory > _peakMemory)
            {
                _peakMemory = currentMemory;
            }
        }
    }

    public void PrintReport()
    {
        _stopwatch.Stop();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - _initialMemory;
        var peakMemoryUsed = _peakMemory - _initialMemory;

        var gcCollections0 = GC.CollectionCount(0) - _initialGcCollections0;
        var gcCollections1 = GC.CollectionCount(1) - _initialGcCollections1;
        var gcCollections2 = GC.CollectionCount(2) - _initialGcCollections2;

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("                  PERFORMANCE REPORT                   ");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine("⏱️  Execution Time:");
        Console.WriteLine($"   Total:           {_stopwatch.Elapsed.TotalSeconds:F3} seconds");
        Console.WriteLine($"   Per entity:      {(_stopwatch.Elapsed.TotalMilliseconds / _entitiesProcessed):F3} ms");
        Console.WriteLine($"   Throughput:      {(_entitiesProcessed / _stopwatch.Elapsed.TotalSeconds):F0} entities/sec");
        Console.WriteLine();

        Console.WriteLine("💾 Memory Usage:");
        Console.WriteLine($"   Initial:         {FormatBytes(_initialMemory)}");
        Console.WriteLine($"   Final:           {FormatBytes(finalMemory)}");
        Console.WriteLine($"   Used:            {FormatBytes(memoryUsed)}");
        Console.WriteLine($"   Peak:            {FormatBytes(peakMemoryUsed)}");
        Console.WriteLine();

        Console.WriteLine("🗑️  Garbage Collection:");
        Console.WriteLine($"   Gen 0:           {gcCollections0} collections");
        Console.WriteLine($"   Gen 1:           {gcCollections1} collections");
        Console.WriteLine($"   Gen 2:           {gcCollections2} collections");
        Console.WriteLine();

        Console.WriteLine("📊 Processing Stats:");
        Console.WriteLine($"   Entities:        {_entitiesProcessed:N0}");
        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }

    public void Dispose()
    {
        _stopwatch?.Stop();
    }
}
