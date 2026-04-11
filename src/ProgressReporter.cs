using System;

namespace Bingosoft.Net.IfcDetail;

internal sealed class ProgressReporter
{
    private readonly long _totalEntities;
    private long _processedEntities;
    private int _lastReportedPercent = -1;
    private readonly object _lock = new object();

    public ProgressReporter(long totalEntities)
    {
        _totalEntities = totalEntities;
        _processedEntities = 0;

        Console.WriteLine();
        Console.WriteLine("Processing: 0%");
    }

    public void ReportProgress()
    {
        lock (_lock)
        {
            _processedEntities++;

            // Calculate percentage
            var percent = (int)((_processedEntities * 100) / _totalEntities);

            // Only update if percentage changed
            if (percent != _lastReportedPercent && percent <= 100)
            {
                _lastReportedPercent = percent;

                // Move cursor to beginning of line and overwrite
                Console.Write($"\rProcessing: {percent}%");

                // If completed, add newline
                if (percent >= 100)
                {
                    Console.WriteLine();
                }
            }
        }
    }

    public void Complete()
    {
        lock (_lock)
        {
            if (_lastReportedPercent < 100)
            {
                Console.Write("\rProcessing: 100%");
                Console.WriteLine();
            }
        }
    }
}
