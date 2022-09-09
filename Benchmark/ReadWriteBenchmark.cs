using Common;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
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

    /// <inheritdoc cref="RunAsync(DriveInfo)"/>
    [SupportedOSPlatform("windows")]
    public async Task<(BenchmarkResult Read, BenchmarkResult Write)> RunAsync(string driveName) =>
        await RunAsync(new DriveInfo(driveName));

    /// <exception cref="IOException" />
    /// <exception cref="UnauthorizedAccessException" />
    /// <exception cref="InsufficientMemoryException" />
    [SupportedOSPlatform("windows")]
    public async Task<(BenchmarkResult Read, BenchmarkResult Write)> RunAsync(DriveInfo drive)
    {
        if (drive.AvailableFreeSpace < _dataSize)
            throw new InsufficientMemoryException("Not enough available free space on the specified drive.");

        // Required for writing to the system drive as writing to its root is forbidden.
        return await Directory.CreateDirectory(
            Path.Combine(drive.Name, Path.GetRandomFileName())
            ).DeleteAfterAsync(RunBenchmarkInDirectoryAsync);
    }

    [SupportedOSPlatform("windows")]
    async Task<(BenchmarkResult Read, BenchmarkResult Write)> RunBenchmarkInDirectoryAsync(DirectoryInfo directory)
    {
        TimeSpan readTime, writeTime;
        using (var safeFileHandle = new SafeFileHandle(IOHelper.CreateUnbufferedFile(directory.FullName), true))
        {
            var fileHandle = safeFileHandle.DangerousGetHandle();
            writeTime = await RunWriteBenchmarkAsync(fileHandle);
            readTime = await RunReadBenchmarkAsync(fileHandle);
        }
        return (new(_dataSize, readTime), new(_dataSize, writeTime));
    }

    [SupportedOSPlatform("windows")]
    async Task<TimeSpan> RunWriteBenchmarkAsync(IntPtr fileHandle)
    {
        var sw = Stopwatch.StartNew();
        await Task.Run(() => RunWriteBenchmark(fileHandle));
        sw.Stop();
        return sw.Elapsed;
    }

    [SupportedOSPlatform("windows")]
    void RunWriteBenchmark(IntPtr fileHandle)
    {
        var dataChunk = new byte[_chunkSize];
        long actualChunkSize = _chunkSize;
        long totalBytesWritten = 0;
        while (totalBytesWritten < _dataSize)
        {
            if (_dataSize - totalBytesWritten < _chunkSize)
                actualChunkSize = _dataSize - totalBytesWritten;
            if (IOHelper.WriteFile(fileHandle, dataChunk[..(Index)actualChunkSize], out var bytesWritten, offset: totalBytesWritten))
            { totalBytesWritten += bytesWritten; }
        }
    }

    [SupportedOSPlatform("windows")]
    async Task<TimeSpan> RunReadBenchmarkAsync(IntPtr fileHandle)
    {
        var sw = Stopwatch.StartNew();
        await Task.Run(() => RunReadBenchmark(fileHandle));
        sw.Stop();
        return sw.Elapsed;
    }

    [SupportedOSPlatform("windows")]
    void RunReadBenchmark(IntPtr fileHandle)
    {
        var outputBuffer = new byte[_chunkSize];
        long totalBytesRead = 0;
        while (totalBytesRead < _dataSize)
        {
            if (IOHelper.ReadFile(fileHandle, outputBuffer, out var bytesRead, offset: totalBytesRead))
            { totalBytesRead += bytesRead; }
        }
    }
}
