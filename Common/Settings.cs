namespace Common;

public static class Settings
{
    public static int PingDelaySec => 1; // Get<int>("pingdelay");
    public static string NodeExePath => "/tmp/kate"; // Get<string>("nodeexepath");
    public static int ListenPort => 5000; // Get<int>("listenport");

    // TODO: sqliteconnection stuff

    static T Get<T>(string name)
    {
        // TODO:
        return default!;
    }
}
