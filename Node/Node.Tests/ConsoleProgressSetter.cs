namespace Node.Tests;

public class ConsoleProgressSetter : IProgressSetter
{
    public required ILogger<ConsoleProgressSetter> Logger { get; init; }

    public void Set(double progress) => Logger.LogInformation("Task progress: " + progress);
}