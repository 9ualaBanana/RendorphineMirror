using System.Diagnostics;
using System.Management.Automation;

namespace Node.Plugins;

public static class CondaManager
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public static string WrapWithInitEnv(string name, string script) => $"""
        {GetInitEnvScript(name, _ => throw new Exception("Conda environment was not initialized"))}
        {script}
        """;

    static string GetInitEnvScript(string name, Func<string, string> createEnvFunc)
    {
        var conda = PluginType.Conda.GetInstance();
        if (conda is null) throw new Exception("Could not find conda plugin");

        var envdir = Directories.Created(Init.ConfigDirectory, "conda", name.ToLowerInvariant());
        var envcreated =
            File.Exists(Path.Combine(envdir, "python.exe")) || File.Exists(Path.Combine(envdir, "python"))
            || File.Exists(Path.Combine(envdir, "bin", "python.exe")) || File.Exists(Path.Combine(envdir, "bin", "python"));

        return $"""
            # initialize conda
            (& "{conda.Path}" "shell" "hook" "--shell=powershell") | Out-String | ?{"{$_}"} | Invoke-Expression

            {(envcreated ? string.Empty : createEnvFunc(envdir))}
            micromamba activate '{envdir}'
            """;
    }

    public static void Initialize(string name, string pyversion, IEnumerable<string> requirements, IEnumerable<string> channels)
    {
        var script = $"""
            {GetInitEnvScript(name, envdir => $"micromamba create -y -p '{envdir}' python={pyversion} {string.Join(' ', channels.Select(c => $"-c '{c}'"))}")}
            micromamba install -y {string.Join(' ', requirements.Select(r => $"'{r}'"))} {string.Join(' ', channels.Select(c => $"-c '{c}'"))}
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
            // maybe add --json to conda install and read json process here
            // {"fetch":"qt-main-5.15.8       | 50.0 MB   | ","finished":false,"maxval":1,"progress":0.630619}

            log();
        }
        void onerr(object obj, Action log)
        {
            log();
            throw new Exception(obj.ToString());
        }
    }
}
