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

    public string GetActivateScript(string condapath, string envname) => $"""
        (& '{condapath}' shell hook --shell powershell --root-prefix '{Dirs.DataDir("conda")}') | Out-String | Invoke-Expression
        micromamba activate '{GetEnvironmentDirectory(envname)}'
        """;


    /// <remarks> Overwrites the environments if exists. </remarks>
    public void InitializeEnvironment(string condapath, string name, string pyversion,
        IReadOnlyCollection<string> condarequirements, IReadOnlyCollection<string> condachannels, IReadOnlyCollection<string> piprequirements, IReadOnlyCollection<string> piprequirementfiles,
        string cwd)
    {
        {
            var log = $"Initializing conda environment {name}"
                + $"; python={pyversion}"
                + $"; condareq {string.Join(' ', condarequirements)}"
                + $"; condac {string.Join(' ', condachannels)}"
                + $"; pipreq {string.Join(' ', piprequirements)}"
                + $"; pipreqfiles {string.Join(' ', piprequirementfiles)}";

            Logger.LogInformation(log);
        }


        var script = $"""
            Set-Location '{Path.GetFullPath(cwd)}'
            & '{condapath}' create -y --json -p '{GetEnvironmentDirectory(name)}' 'python={pyversion}' {string.Join(' ', condarequirements.Select(r => $"'{r}'"))} {string.Join(' ', condachannels.Select(c => $"-c '{c}'"))}
            {GetActivateScript(condapath, name)}
            """;

        if (piprequirements.Count != 0 || piprequirementfiles.Count != 0)
        {
            var sc = $"\n pip install";
            if (piprequirements.Count != 0)
                sc += $" {string.Join(' ', piprequirements.Select(r => $"'{r}'"))}";
            if (piprequirementfiles.Count != 0)
                sc += $" {string.Join(' ', piprequirementfiles.Select(r => $"-r '{r}'"))}";

            script += sc;
        }


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
