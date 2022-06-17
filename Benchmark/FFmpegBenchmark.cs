using System.Diagnostics;

namespace Benchmark;

public class FFmpegBenchmark
{
    readonly string _ffmpegExecutablePath;
    readonly string _inputFilePath;

    public FFmpegBenchmark(string fileName, string ffmpegPath = "ffmpeg")
    {
        _ffmpegExecutablePath = GetPathToExecutable(ffmpegPath);
        _inputFilePath = fileName;
    }

    static string GetPathToExecutable(string path)
    {
        if (!Directory.Exists(path)) return path;

        return Path.Combine(path, "ffmpeg");
    }

    public async Task<BenchmarkResult> RunOnCpuAsync() => await RunAsync();

    public async Task<BenchmarkResult> RunOnGpuAsync() => await RunAsync("-hwaccel cuda");

    public async Task<BenchmarkResult> RunAsync(string? additionalArguments = null)
    {
        if (FFmpegIsNotInstalled)
            throw new FileNotFoundException("FFmpeg executable is not found.");
        if (!File.Exists(_inputFilePath))
            throw new FileNotFoundException("Specified file does not exist.", _inputFilePath);

        return new(
            (await File.ReadAllBytesAsync(_inputFilePath)).LongLength,
            await RunFFmpegProcessingAsync(additionalArguments ?? string.Empty)
            );
    }

    bool FFmpegIsNotInstalled => Process.Start(new ProcessStartInfo(_ffmpegExecutablePath, "-version") { CreateNoWindow = true }) is null;

    async Task<TimeSpan> RunFFmpegProcessingAsync(string additionalArguments)
    {
        var processStartInfo = new ProcessStartInfo(_ffmpegExecutablePath, $"-benchmark {additionalArguments} -i {_inputFilePath} -f null -")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        using var processOutputBuffer = new MemoryStream();
        using (var bufferWriter = new StreamWriter(processOutputBuffer, leaveOpen: true) { AutoFlush = true })
        {
            process.ErrorDataReceived += (s, e) => { bufferWriter.WriteLine(e.Data); };

            process.Start();
            process.BeginErrorReadLine();
            await process!.WaitForExitAsync();
        }

        if (process.ExitCode != 0)
            throw new Exception($"FFmpeg process exited with {process.ExitCode} status code.");

        processOutputBuffer.Position = 0;
        return ParseFFmpegOutput(await new StreamReader(processOutputBuffer).ReadToEndAsync());
    }

    static TimeSpan ParseFFmpegOutput(string output)
    {
        var processingTime = string.Join(string.Empty, output.Split("rtime=", 2)[1].TakeWhile(c => c != 's'));
        return TimeSpan.FromSeconds(double.Parse(processingTime));
    }
}
