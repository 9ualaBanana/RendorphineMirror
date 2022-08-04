namespace Node;

public static class Extensions
{
    public static void LogInfo(this ReceivedTask task, string text) => Log.Information($"[Task {task.Id}] {text}");
    public static void LogErr(this ReceivedTask task, string text) => Log.Error($"[Task {task.Id}] {text}");
    public static void LogErr(this ReceivedTask task, Exception ex) => task.LogErr(ex.ToString());
}
