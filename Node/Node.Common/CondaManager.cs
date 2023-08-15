using System.Management.Automation;

namespace Node.Common;

public static class CondaManager
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public static bool IsEnvironmentCreated(string name)
    {
        var envdir = GetEnvironmentDirectory(name);

        return File.Exists(Path.Combine(envdir, "python.exe")) || File.Exists(Path.Combine(envdir, "python"))
            || File.Exists(Path.Combine(envdir, "bin", "python.exe")) || File.Exists(Path.Combine(envdir, "bin", "python"));
    }

    public static string GetEnvironmentDirectory(string envname) => Path.Combine(Directories.Data, "conda", envname.ToLowerInvariant());

    public static string GetRunInEnvironmentScript(string condapath, string envname, string command) =>
        $"& '{condapath}' run -p '{GetEnvironmentDirectory(envname)}' {command}";


    /// <remarks> Overwrites the environments if exists. </remarks>
    public static void InitializeEnvironment(string condapath, string name, string pyversion, IReadOnlyCollection<string> requirements, IReadOnlyCollection<string> channels, IReadOnlyCollection<string>? piprequirements)
    {
        {
            var log = $"Initializing conda environment {name} with python={pyversion} {string.Join(' ', requirements)}";
            log += $"; channels {string.Join(' ', channels)}";
            if (piprequirements is not null)
                log += $"; pip {string.Join(' ', piprequirements)}";

            Logger.Info(log);
        }

        var script = $"""
            & '{condapath}' create -y --json -p '{GetEnvironmentDirectory(name)}' 'python={pyversion}' {string.Join(' ', requirements.Select(r => $"'{r}'"))} {string.Join(' ', channels.Select(c => $"-c '{c}'"))}
            {(piprequirements is null ? null : GetRunInEnvironmentScript(condapath, name, $"pip install {string.Join(' ', piprequirements.Select(r => $"'{r}'"))}"))}
            """;

        try
        {
            var sw = Stopwatch.StartNew();
            PowerShellInvoker.Invoke(script, onread, onerr, LogManager.GetLogger($"Conda init {name}").AsLoggable());
            Logger.Info($"Conda environment '{name}' initialized succesfully in {sw.Elapsed}.");
        }
        catch (Exception ex)
        {
            Logger.Info($"Could not initialize conda environment '{name}': {ex}");
        }


        void onread(PSObject obj, Action log)
        {
            // TODO: read json
            log();
        }
        void onerr(object obj, Action log)
        {
            log();
            throw new Exception(obj.ToString());
        }
    }
}
