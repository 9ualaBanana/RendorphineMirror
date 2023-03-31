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

        var envdir = Path.Combine(Init.ConfigDirectory, "conda", name.ToLowerInvariant());
        var envcreated =
            File.Exists(Path.Combine(envdir, "python.exe")) || File.Exists(Path.Combine(envdir, "python"))
            || File.Exists(Path.Combine(envdir, "bin", "python.exe")) || File.Exists(Path.Combine(envdir, "bin", "python"));

        #region fix for https://github.com/mamba-org/mamba/issues/2157
        var hookfix = """
            function Invoke-Mamba() {
                # Don't use any explicit args here, we'll use $args and tab completion
                # so that we can capture everything, INCLUDING short options (e.g. -n).
                if ($Args.Count -eq 0) {
                    # No args, just call the underlying mamba executable.
                    & $Env:MAMBA_EXE;
                }
                else {
                    $Command = $Args[0];
                    if ($Args.Count -ge 2) {
                        $OtherArgs = $Args[1..($Args.Count - 1)];
                    } else {
                        $OtherArgs = @();
                    }
                    switch ($Command) {
                        "activate" {
                            Enter-MambaEnvironment @OtherArgs;
                        }
                        "deactivate" {
                            Exit-MambaEnvironment;
                        }
                        "self-update" {
                            & $Env:MAMBA_EXE $Command @OtherArgs;
                            $MAMBA_EXE_BKUP = $Env:MAMBA_EXE + ".bkup";
                            if (Test-Path $MAMBA_EXE_BKUP) {
                                Remove-Item $MAMBA_EXE_BKUP
                            }
                        }
                        default {
                            # There may be a command we don't know want to handle
                            # differently in the shell wrapper, pass it through
                            # verbatim.
                            & $Env:MAMBA_EXE $Command @OtherArgs;

                            # reactivate environment
                            if (@("install", "update", "remove").contains($Command))
                            {
                                $currentEnv = $Env:CONDA_DEFAULT_ENV;
                                Exit-MambaEnvironment;
                                Enter-MambaEnvironment $currentEnv;
                            }
                        }
                    }
                }
            }
            """;
        #endregion

        return $"""
            # initialize conda
            (& "{conda.Path}" "shell" "hook" "--shell=powershell") | Out-String | ?{"{$_}"} | Invoke-Expression
            {hookfix}

            {(envcreated ? string.Empty : createEnvFunc(envdir))}
            micromamba activate '{envdir}'
            """;
    }

    public static void Initialize(string name, string pyversion, IEnumerable<string> requirements, IEnumerable<string> channels, IEnumerable<string>? piprequirements)
    {
        var script = $"""
            {GetInitEnvScript(name, envdir => $"micromamba create -y -p '{envdir}' python={pyversion} {string.Join(' ', channels.Select(c => $"-c '{c}'"))}")}
            micromamba install -y --json {string.Join(' ', requirements.Select(r => $"'{r}'"))} {string.Join(' ', channels.Select(c => $"-c '{c}'"))}
            {(piprequirements is null ? null : $"pip install {string.Join(' ', piprequirements.Select(r => $"'{r}'"))}")}
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
