using System.Management.Automation;

namespace Node.Common;

public class CondaManager
{
    public required PowerShellInvoker PowerShellInvoker { get; init; }
    public required DataDirs Dirs { get; init; }
    public required ILogger<CondaManager> Logger { get; init; }

    public bool IsEnvironmentCreated(string name)
    {
        var envdir = GetEnvironmentDirectory(name);

        return File.Exists(Path.Combine(envdir, "python.exe")) || File.Exists(Path.Combine(envdir, "python"))
            || File.Exists(Path.Combine(envdir, "bin", "python.exe")) || File.Exists(Path.Combine(envdir, "bin", "python"));
    }

    public string GetEnvironmentDirectory(string envname) => Dirs.DataDir(Path.Combine("conda", envname.ToLowerInvariant()), false);

    public string GetRunInEnvironmentScript(string condapath, string envname, string command) =>
        $"& '{condapath}' run -p '{GetEnvironmentDirectory(envname)}' {command}";


    /// <remarks> Overwrites the environments if exists. </remarks>
    public void InitializeEnvironment(string condapath, string name, string pyversion, IReadOnlyCollection<string> requirements, IReadOnlyCollection<string> channels, IReadOnlyCollection<string>? piprequirements)
    {
        {
            var log = $"Initializing conda environment {name} with python={pyversion} {string.Join(' ', requirements)}";
            log += $"; channels {string.Join(' ', channels)}";
            if (piprequirements is not null)
                log += $"; pip {string.Join(' ', piprequirements)}";

            Logger.LogInformation(log);
        }

        var script = $"""
            & '{condapath}' create -y --json -p '{GetEnvironmentDirectory(name)}' 'python={pyversion}' {string.Join(' ', requirements.Select(r => $"'{r}'"))} {string.Join(' ', channels.Select(c => $"-c '{c}'"))}
            {(piprequirements is null ? null : GetRunInEnvironmentScript(condapath, name, $"pip install {string.Join(' ', piprequirements.Select(r => $"'{r}'"))}"))}
            """;

        try
        {
            var sw = Stopwatch.StartNew();

            using var _logscope = Logger.BeginScope($"Conda init {name}");
            PowerShellInvoker.Invoke(script, onread, onerr);
            Logger.LogInformation($"Conda environment '{name}' initialized succesfully in {sw.Elapsed}.");
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Could not initialize conda environment '{name}': {ex}");
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
