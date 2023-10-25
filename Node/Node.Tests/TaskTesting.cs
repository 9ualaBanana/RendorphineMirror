namespace Node.Tests;

public static class TaskTesting
{
    public static string TempDirFor(string action)
    {
        var dir = Path.GetFullPath("temp/" + action);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);

        return dir;
    }
}
