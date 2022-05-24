namespace Common;

public static class Settings
{
    public static int PingDelaySec => 1; // Get<int>("pingdelay");
    public static string NodeExePath => "/tmp/kate"; // Get<string>("nodeexepath");

    // TODO: sqliteconnection stuff

    static T Get<T>(string name)
    {
        // TODO:
        return default!;
    }
}
