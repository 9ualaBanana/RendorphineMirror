using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace Benchmark;

public class ReadWriteBenchmark : IDisposable
{
    readonly byte[] _bytesToWrite;
    readonly byte[] _readOutput;

    public ReadWriteBenchmark(uint size)
    {
        _bytesToWrite = new byte[size];
        _readOutput = new byte[size];
    }

    /// <exception cref="IOException" />
    /// <exception cref="UnauthorizedAccessException" />
    /// <exception cref="InsufficientMemoryException" />
    [SupportedOSPlatform("windows")]
    public async Task<(BenchmarkResult Read, BenchmarkResult Write)> RunAsync(string driveName)
    {
        var drive = new DriveInfo(driveName);
        if (drive.AvailableFreeSpace < _bytesToWrite.Length)
            throw new InsufficientMemoryException("Not enough available free space on the specified drive.");

        TimeSpan readTime, writeTime;
        // Required for writing to the system drive as writing to its root is forbidden.
        var tempDir = Directory.CreateDirectory(
            Path.Combine(drive.Name, Path.GetRandomFileName())
            );
        var fileHandle = FileHelper.CreateUnbufferedFile(tempDir.FullName);

        using var safeFileHandle = new SafeFileHandle(fileHandle, true);
        {
            writeTime = await RunWriteBenchmarkAsync(safeFileHandle);
            readTime = await RunReadBenchmarkAsync(safeFileHandle);
        }
        tempDir.Delete();

        return (new(_bytesToWrite.Length, readTime), new(_readOutput.Length, writeTime));
    }

    [SupportedOSPlatform("windows")]
    async Task<TimeSpan> RunWriteBenchmarkAsync(SafeFileHandle safeFileHandle)
    {
        var sw = Stopwatch.StartNew();
        await Task.Run(() => FileHelper.WriteFile(safeFileHandle.DangerousGetHandle(), _bytesToWrite));
        sw.Stop();
        return sw.Elapsed;
    }

    [SupportedOSPlatform("windows")]
    async Task<TimeSpan> RunReadBenchmarkAsync(SafeFileHandle safeFileHandle)
    {
        var sw = Stopwatch.StartNew();
        await Task.Run(() => FileHelper.ReadFile(safeFileHandle.DangerousGetHandle(), _readOutput));
        sw.Stop();
        return sw.Elapsed;
    }

    public void Dispose()
    {
        GC.Collect();
        GC.SuppressFinalize(this);
    }
}
