namespace Uploader;

public static class Extensions
{
    public static void WriteLine(this ConsoleColor fg, string text)
    {
        var prevcolor = Console.ForegroundColor;
        Console.ForegroundColor = fg;
        Console.WriteLine(text);
        Console.ForegroundColor = prevcolor;
    }

    public static Process StartProcess(this ProcessStartInfo info)
    {
        ConsoleColor.Green.WriteLine($"Starting {info.FileName} {info.Arguments} {string.Join(' ', info.ArgumentList)}");
        return Process.Start(info) ?? throw new Exception($"Could not start {info.FileName}");
    }
    public static void StartProcessWait(this ProcessStartInfo info)
    {
        var proc = StartProcess(info);
        proc.WaitForExit();

        if (proc.ExitCode != 0) throw new Exception($"Non-zero exit code from {info.FileName}: {proc.ExitCode}");
    }
}
