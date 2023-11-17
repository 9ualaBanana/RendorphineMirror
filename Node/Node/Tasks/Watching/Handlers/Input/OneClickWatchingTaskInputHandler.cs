
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Node.Listeners;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickWatchingTaskInputHandler : WatchingTaskInputHandler<OneClickWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    public required IPluginList PluginList { get; init; }

    public override void StartListening() => StartThreadRepeated(5_000, RunOnce);

    public async Task RunOnce()
    {
        Directory.CreateDirectory(Input.TestMzpDirectory);

        try
        {
            var betamzp = Directory.GetFiles(Input.TestMzpDirectory)
                .Where(p => Path.GetFileName(p).StartsWith("oneclick") && p.EndsWith(".mzp"))
                .Max();

            if (betamzp is not null)
            {
                var plugin = new Plugin(PluginType.OneClick, Path.GetFileNameWithoutExtension(betamzp)!.Substring("oneclickexport.v".Length), betamzp);
                await Run(plugin, Input.TestInputDirectory, Input.TestOutputDirectory, Input.TestLogDirectory);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        try
        {
            var plugin = PluginList.GetPlugin(PluginType.OneClick);
            await Run(plugin, Input.InputDirectory, Input.OutputDirectory, Input.LogDirectory);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    public async Task Run(Plugin mzp, string input, string output, string log)
    {
        var max = PluginList.GetPlugin(PluginType.Autodesk3dsMax);
        var unity = PluginList.GetPlugin(PluginType.Unity);

        Directory.CreateDirectory(output);
        Directory.CreateDirectory(input);
        Directory.CreateDirectory(log);

        var currentversion = Directory.GetFiles(output, "*.mzp")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault()
            ?.Substring("oneclickexport.v".Length);

        if (currentversion != mzp.Version)
        {
            await Install(mzp, max);
            await CheckInstallation(mzp, max);
            await MoveOldVersion(mzp, output, currentversion);
        }

        var unityTemplatesDir = @"C:\\OneClickUnityDefaultProjects";
        await new ProcessLauncher("git", "pull")
        {
            Logging = { ILogger = Logger },
            ThrowOnStdErr = false,
            WorkingDirectory = unityTemplatesDir,
        }.ExecuteAsync();
        var unityTemplatesCommitHash = (await new ProcessLauncher("git", "rev-parse", "--verify", "HEAD")
        {
            Logging = { ILogger = Logger },
            ThrowOnStdErr = false,
            WorkingDirectory = unityTemplatesDir,
        }.ExecuteFullAsync()).Trim();

        foreach (var zip in Directory.GetFiles(input, "*.zip"))
            await ProcessArchive(zip, output, log, max, unity, unityTemplatesDir, unityTemplatesCommitHash);

        if (unityTemplatesCommitHash != Input.UnityProjectsCommitHash)
        {
            Input.UnityProjectsCommitHash = unityTemplatesCommitHash;
            SaveTask();
        }
    }

    async Task ProcessArchive(string zip, string output, string log, Plugin max, Plugin unity, string unityTemplatesDir, string unityTemplatesCommitHash)
    {
        using var _logscope = Logger.BeginScope($"Processing {Path.GetFileName(zip)}");

        var resultDir = Path.Combine(output, Path.GetFileNameWithoutExtension(zip));
        var resultUnityDir = Path.Combine(resultDir, "unity");
        var resultUnityAssetsDir = Path.Combine(resultUnityDir, "Assets");

        var runmax = !Directory.Exists(resultDir);
        var rununity = runmax || Input.UnityProjectsCommitHash != unityTemplatesCommitHash;

        if (runmax)
        {
            using var _ = Logger.BeginScope($"3dsmax");
            Logger.Info($"Procesing {zip}");

            Logger.Info($"Extracting");
            ZipFile.ExtractToDirectory(zip, resultDir);
            Logger.Info("Extracted");
            var scenefile = Directory.GetFiles(resultDir, "*.max", SearchOption.AllDirectories).MaxBy(File.GetLastWriteTimeUtc).ThrowIfNull("No .max file found");
            Logger.Info($"Scene file: {scenefile}");

            Directory.CreateDirectory(resultUnityAssetsDir);
            Logger.Info($"Target directory: {resultUnityAssetsDir}");

            await RunMax(scenefile, zip, output, log, max, resultUnityAssetsDir);
            Logger.Info("Success.");
        }

        if (false && rununity)
        {
            using var _ = Logger.BeginScope($"Unity");
            await RunUnity(unity, unityTemplatesDir, resultUnityDir, resultUnityAssetsDir);
            Logger.Info("Success.");
        }
    }

    async Task RunMax(string scenefile, string zip, string output, string log, Plugin max, string resultUnityAssetsDir)
    {
        var launcher = new ProcessLauncher(max.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Arguments =
            {
                // minimized, dialog boxes suppressed
                "-ms", "-silent",

                // log path
                "-log", Directories.NumberedNameInDirectory(log, "log{0:0000}.log"),

                // script parameters
                /*
                int - target engine; 1 = unreal, 2 = unity
                string - output dir; should already exist
                int - existing texture mode; 1 = skip copying, 2 = 256px, 3=512, 4=1024, 5=2048, 6=4096
                int - bake texture mode; 1 = skip baking, 2 = 128px, 3=256, 4=512, 5=1024, 6=2048, 7=4096
                bool int int - render cameras (true\false) and frame width height (always should be specified)
                (1.35+) bool - deploy importer from internal zip
                */
                "-mxs", $"oneclickexport.oc000 2 @\"{resultUnityAssetsDir}\" 3 3 true 960 540 false",

                // scene to export
                scenefile.Replace('\\', '/'),
            },
        };

        Logger.Info("Launching 3dsmax");
        await launcher.ExecuteAsync();
        Logger.Info("Conversion completed");

        await ValidateConversionSuccessful(zip, output);
    }
    async Task RunUnity(Plugin unity, string unityTemplatesDir, string resultUnityDir, string resultUnityAssetsDir)
    {
        var unityTemplateNames = new[] { "OCHDRP22+" };

        foreach (var unityTemplateName in unityTemplateNames)
        {
            var path = Path.Combine(resultUnityDir, unityTemplateName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }


        foreach (var unityTemplateName in unityTemplateNames)
        {
            using var _ = Logger.BeginScope(unityTemplateName);

            var unityProjectDir = Path.Combine(unityTemplatesDir, unityTemplateName);
            Directories.Copy(unityProjectDir, Path.Combine(resultUnityDir, unityTemplateName));
            Directories.Copy(resultUnityAssetsDir, Path.Combine(resultUnityDir, unityTemplateName, "Assets"));

            Logger.Info("Launching unity");

            //NonAdminRunner.RunAsDesktopUserWaitForExit(unity.Path, $"-projectPath \"{unityProjectDir}\" -executeMethod OCBatchScript.StartBake");
            var launcher = new ProcessLauncher(unity.Path)
            {
                Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
                Arguments =
                {
                    "-projectPath", unityProjectDir,
                    "-executeMethod", "OCBatchScript.StartBake",
                },
            };

            await launcher.ExecuteAsync();
            Logger.Info("Completed");
        }

        Logger.Info("Completed");
    }

    async Task ValidateConversionSuccessful(string zip, string output)
    {
        Logger.Info("Validating conversion");

        var dir = Path.Combine(output, Path.GetFileNameWithoutExtension(zip), "unity", "Assets");
        if (!Directory.Exists(dir))
            throw new Exception("Result directory does not exists");

        var logfiles = Directory.GetDirectories(dir)
            .Select(dir => Path.Combine(dir, Path.GetFileName(dir) + ".txt"))
            .Where(File.Exists)
            .ToArray();

        if (logfiles.Length == 0)
            throw new Exception($"Log file was not found in {dir}");

        foreach (var logfile in logfiles)
        {
            var data = await File.ReadAllTextAsync(logfile);
            if (data.ContainsOrdinal("Export completed."))
            {
                Logger.Info("Conversion successful");
                return;
            }
        }

        throw new Exception("'Export completed.' was not found in the log");
    }

    async Task Install(Plugin mzp, Plugin max)
    {
        Logger.Info("Installing the plugin");

        // fix for vray not being silent enough
        const string vraySilentFix = "if setVRaySilentMode != undefined then setVRaySilentMode()";
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(max.Path).ThrowIfNull(), "scripts", "Startup", "oneclicksilent.ms"), vraySilentFix);

        foreach (var process in Process.GetProcessesByName("3dsmax"))
        {
            try { process.Kill(); }
            catch { }
        }

        var launcher = new ProcessLauncher(max.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Timeout = TimeSpan.FromMinutes(5),
            Arguments = { "-ms", "-silent", "-mxs", $"fileIn @\"{mzp.Path}\"" },
        };
        await launcher.ExecuteAsync();

        Logger.Info("Plugin installed");
    }


    async Task CheckInstallation(Plugin mzp, Plugin max)
    {
        Logger.Info("Checking plugin installation");

        using var reader = File.OpenRead(mzp.Path);
        var entry = new ZipArchive(reader).GetEntry("oneclickreadme.txt").ThrowIfNull("OneClick version was not found in mzp");
        using var entrystream = new StreamReader(entry.Open());
        var expectedversion = await entrystream.ReadToEndAsync();

        // %localAppData%\Autodesk\3dsMax\20?? - 64bit\ENU\scripts\startup\oneclickreadme.txt
        var installedpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Autodesk", "3dsMax", $"{max.Version} - 64bit", "ENU", "scripts", "startup", "oneclickreadme.txt");

        var installedversion = File.ReadAllText(installedpath);

        if (installedversion != expectedversion)
            throw new Exception($"Invalid mzp installation: versions are not equal ({installedversion} vs {expectedversion})");

        Logger.Info($"Installed plugin version: {installedversion}");
    }

    async Task MoveOldVersion(Plugin mzp, string output, string? currentversion)
    {
        Logger.Info("Moving old dirs");

        if (Directory.Exists(output))
        {
            if (currentversion is null) Directory.Delete("output", true);
            else Directory.Move(output, output + currentversion);
        }

        Directory.CreateDirectory(output);
        var target = Path.Combine(output, Path.GetFileName(mzp.Path));
        File.Copy(mzp.Path, target);

        Logger.Info($"Old output dir moved to {target}");
    }


    class OneClickListener : ListenerBase
    {
        protected override ListenTypes ListenType => ListenTypes.Public;

        public OneClickListener(ILogger<OneClickListener> logger) : base(logger) { }

        protected override async ValueTask Execute(HttpListenerContext context)
        {

        }
    }


    static class NonAdminRunner
    {
        class ProcessWaitHandle : WaitHandle
        {
            public ProcessWaitHandle(IntPtr processHandle) => SafeWaitHandle = new SafeWaitHandle(processHandle, false);
        }

        public static void RunAsDesktopUserWaitForExit(string fileName, string args)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));

            // To start process as shell user you will need to carry out these steps:
            // 1. Enable the SeIncreaseQuotaPrivilege in your current token
            // 2. Get an HWND representing the desktop shell (GetShellWindow)
            // 3. Get the Process ID(PID) of the process associated with that window(GetWindowThreadProcessId)
            // 4. Open that process(OpenProcess)
            // 5. Get the access token from that process (OpenProcessToken)
            // 6. Make a primary token with that token(DuplicateTokenEx)
            // 7. Start the new process with that primary token(CreateProcessWithTokenW)

            var hProcessToken = IntPtr.Zero;
            // Enable SeIncreaseQuotaPrivilege in this process.  (This won't work if current process is not elevated.)
            try
            {
                var process = GetCurrentProcess();
                if (!OpenProcessToken(process, 0x0020, ref hProcessToken))
                    return;

                var tkp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new LUID_AND_ATTRIBUTES[1]
                };

                if (!LookupPrivilegeValue(null!, "SeIncreaseQuotaPrivilege", ref tkp.Privileges[0].Luid))
                    return;

                tkp.Privileges[0].Attributes = 0x00000002;

                if (!AdjustTokenPrivileges(hProcessToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero))
                    return;
            }
            finally
            {
                CloseHandle(hProcessToken);
            }

            // Get an HWND representing the desktop shell.
            // CAVEATS:  This will fail if the shell is not running (crashed or terminated), or the default shell has been
            // replaced with a custom shell.  This also won't return what you probably want if Explorer has been terminated and
            // restarted elevated.
            var hwnd = GetShellWindow();
            if (hwnd == IntPtr.Zero)
                return;

            var hShellProcess = IntPtr.Zero;
            var hShellProcessToken = IntPtr.Zero;
            var hPrimaryToken = IntPtr.Zero;
            try
            {
                // Get the PID of the desktop shell process.
                if (GetWindowThreadProcessId(hwnd, out var dwPID) == 0)
                    return;

                // Open the desktop shell process in order to query it (get the token)
                hShellProcess = OpenProcess(ProcessAccessFlags.QueryInformation, false, dwPID);
                if (hShellProcess == IntPtr.Zero)
                    return;

                // Get the process token of the desktop shell.
                if (!OpenProcessToken(hShellProcess, 0x0002, ref hShellProcessToken))
                    return;

                var dwTokenRights = 395U;

                // Duplicate the shell's process token to get a primary token.
                // Based on experimentation, this is the minimal set of rights required for CreateProcessWithTokenW (contrary to current documentation).
                if (!DuplicateTokenEx(hShellProcessToken, dwTokenRights, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out hPrimaryToken))
                    return;

                // Start the target process with the new token.
                var si = new STARTUPINFO();
                var pi = new PROCESS_INFORMATION();
                if (!CreateProcessWithTokenW(hPrimaryToken, 0, null!, $"\"{fileName}\" {args}", 0, IntPtr.Zero, Path.GetDirectoryName(fileName)!, ref si, out pi))
                    return;

                new ProcessWaitHandle(pi.hProcess).WaitOne();
            }
            finally
            {
                CloseHandle(hShellProcessToken);
                CloseHandle(hPrimaryToken);
                CloseHandle(hShellProcess);
            }

        }

        #region Interop

        struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [Flags]
        enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool LookupPrivilegeValue(string host, string name, ref LUID pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TOKEN_PRIVILEGES newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);


        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, uint processId);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out IntPtr phNewToken);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessWithTokenW(IntPtr hToken, int dwLogonFlags, string lpApplicationName, string lpCommandLine, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        #endregion
    }
}
