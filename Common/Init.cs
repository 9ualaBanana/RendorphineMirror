namespace Common;

public static class Initializer
{
    public static readonly string AppName;


    static Initializer()
    {
        var appname = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        if (appname is null or "dotnet")
            appname = System.Reflection.Assembly.GetEntryAssembly().ThrowIfNull().GetName().Name.ThrowIfNull();

        AppName = appname;
    }
}