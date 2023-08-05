namespace Uploader;

public record UploadAction(string Source, string Destination) : IAction
{
    static void Start(string exe, params string[] args)
    {
        var pinfo = new ProcessStartInfo(exe);
        foreach (var arg in args)
            pinfo.ArgumentList.Add(arg);

        pinfo.StartProcessWait();
    }
    public static void Upload(string source, string destination)
    {
        if (File.Exists("/bin/rsync"))
            Start("/bin/rsync", "-ravhP", "-T=/tmp", "--exclude", "appsettings.*json", source, destination);
        else Start("scp", "-pr", source, destination);
    }

    public void Invoke() => Upload(Source, Destination);
}
