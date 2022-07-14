using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace Benchmark;

public class ReadWriteBenchmark
{
    readonly long _dataSize;
    const int _chunkSize = 4 * 1024 * 1024;

    public ReadWriteBenchmark(long size)
    {
        _dataSize = size;
    }

    /// <exception cref="IOException" />
    /// <exception cref="UnauthorizedAccessException" />
    /// <exception cref="InsufficientMemoryException" />
    [SupportedOSPlatform("windows")]
    public async Task<(BenchmarkResult Read, BenchmarkResult Write)> RunAsync(string driveName)
    {
        var drive = new DriveInfo(driveName);
        if (drive.AvailableFreeSpace < _dataSize)
            throw new InsufficientMemoryException("Not enough available free space on the specified drive.");

        TimeSpan readTime, writeTime;
        // Required for writing to the system drive as writing to its root is forbidden.
        var tempDir = Directory.CreateDirectory(
            Path.Combine(drive.Name, Path.GetRandomFileName())
            );
        var fileHandle = FileHelper.CreateUnbufferedFile(tempDir.FullName);

        using (var safeFileHandle = new SafeFileHandle(fileHandle, true))
        {
            writeTime = await RunWriteBenchmarkAsync(safeFileHandle);
            readTime = await RunReadBenchmarkAsync(safeFileHandle);
        }
        tempDir.Delete(true);

        return (new (_dataSize, readTime), new (_dataSize, writeTime));
    }

    [SupportedOSPlatform("windows")]
    async Task<TimeSpan> RunWriteBenchmarkAsync(SafeFileHandle safeFileHandle)
    {
        var sw = Stopwatch.StartNew();
        await Task.Run(() =>
        {
            var dataChunk = new byte[_chunkSize];
            long actualChunkSize = _chunkSize;
            long totalBytesWritten = 0;
            while (totalBytesWritten < _dataSize)
            {
                if (_dataSize - totalBytesWritten < _chunkSize) actualChunkSize = _dataSize - totalBytesWritten;
                if (FileHelper.WriteFile(
                    safeFileHandle.DangerousGetHandle(),
                    dataChunk[..(Index)actualChunkSize],
                    out var bytesWritten,
                    totalBytesWritten)
                )
                { totalBytesWritten += bytesWritten; }
            }
        });
        sw.Stop();
        return sw.Elapsed;
    }

    [SupportedOSPlatform("windows")]
    async Task<TimeSpan> RunReadBenchmarkAsync(SafeFileHandle safeFileHandle)
    {
        var sw = Stopwatch.StartNew();
        await Task.Run(() =>
        {
            var outputBuffer = new byte[_chunkSize];
            long totalBytesRead = 0;
            while (totalBytesRead < _dataSize)
            {
                if (FileHelper.ReadFile(
                    safeFileHandle.DangerousGetHandle(),
                    outputBuffer,
                    out var bytesRead,
                    totalBytesRead)
                ) 
                { totalBytesRead += bytesRead; }
            }
        });
        sw.Stop();
        return sw.Elapsed;
    }
}
