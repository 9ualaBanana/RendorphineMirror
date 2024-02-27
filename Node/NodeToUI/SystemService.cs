using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;
using NLog;

namespace NodeToUI;

public static class SystemService
{
    const string SystemctlExe = "/usr/bin/systemctl";
    const string LaunchctlExe = "/usr/bin/launchctl";
    const string ServiceName = "renderfin_pinger";
    static bool Stopped = false;

    static void Initialize(bool useAdminRights, string servicename)
    {
        try { Stop(servicename); }
        catch { }

        var nodeexe = FileList.GetNodeExe();
        var nodeuiexe = FileList.GetNodeUIExe();
        var pingerexe = FileList.GetPingerExe();
        var updaterexe = FileList.GetUpdaterExe();

        CommonExtensions.MakeExecutable(pingerexe, updaterexe, nodeexe, nodeuiexe);
        ExecuteForOs(Windows, Linux, Mac);


        void Windows()
        {
            // to hide a CA1416 warning
            if (!OperatingSystem.IsWindows()) return;

            try
            {
                tryRegister(true);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger()
                    .Error(ex, "Could not register service task, restarting without System.PowerTroubleshooter");

                tryRegister(false);
            }

            void tryRegister(bool addPowerTrigger)
            {
                using var ts = new TaskService();

                var task = ts.NewTask();
                task.RegistrationInfo.Description = " pinger";
                task.Actions.Add(new ExecAction(pingerexe, workingDirectory: Directory.GetCurrentDirectory()));
                if (useAdminRights) task.Principal.RunLevel = TaskRunLevel.Highest;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.Enabled = true;
                task.Settings.WakeToRun = true;
                task.Settings.AllowHardTerminate = true;
                task.Settings.ExecutionTimeLimit = TimeSpan.FromHours(3);

                // trigger immediately & then every minute forever
                task.Triggers.Add(repeated(new RegistrationTrigger()));
                if (useAdminRights) task.Triggers.Add(repeated(new BootTrigger()));

                var logon = new LogonTrigger();
                if (!useAdminRights)
                    logon.UserId = WindowsIdentity.GetCurrent().Name;
                task.Triggers.Add(repeated(logon));

                if (addPowerTrigger)
                {
                    // trigger on unhibernation
                    //try { task.Triggers.Add(repeated(new EventTrigger("Microsoft-Windows-Diagnostics-Performance/Operational", "PowerTroubleshooter", 1))); }
                    //catch { }
                    try { task.Triggers.Add(repeated(new EventTrigger("System", "PowerTroubleshooter", 1))); }
                    catch { }
                }

                ts.RootFolder.RegisterTask(servicename, task.XmlText, createType: TaskCreation.CreateOrUpdate, logonType: TaskLogonType.InteractiveToken);
            }


            static T repeated<T>(T trigger) where T : Trigger, ITriggerDelay
            {
                trigger.Delay = TimeSpan.FromMinutes(1);
                trigger.Repetition = new RepetitionPattern(TimeSpan.FromMinutes(1), TimeSpan.Zero, false);

                return trigger;
            }
        }
        void Linux()
        {
            var service = $"""
                    [Unit]
                    Description=Renderfin pinger

                    [Service]
                    Type=oneshot
                    KillMode=process
                    WorkingDirectory={Path.GetDirectoryName(updaterexe)}
                    ExecStart="{pingerexe}"
                    """;
            var timer = $"""
                    [Unit]
                    Description=Renderfin pinger

                    [Timer]
                    OnActiveSec=1min
                    OnUnitActiveSec=1min

                    [Install]
                    WantedBy=default.target
                    """;

            var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "systemd/user/");
            Directory.CreateDirectory(configdir);
            File.WriteAllText(Path.Combine(configdir, @$"{servicename}.service"), service);
            File.WriteAllText(Path.Combine(configdir, @$"{servicename}.timer"), timer);

            Launch(SystemctlExe, @$"--user daemon-reload");
        }
        void Mac()
        {
            var service = $@"
                    <?xml version=""1.0"" encoding=""UTF-8""?>
                    <!DOCTYPE plist PUBLIC ""-//Apple Computer//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
                    <plist version=""1.0"">
                    <dict>  
                        <key>Label</key>
                        <string>plus.microstock.renderphine-pinger</string>
                        <key>ProgramArguments</key>
                        <array>
                            <string>{pingerexe}</string>
                        </array>
                        <key>RunAtLoad</key>
                        <true/>
                        <key>StartInterval</key>
                        <integer>600</integer>
                    </dict>
                    </plist>
                ";

            var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/LaunchAgents");
            File.WriteAllText(Path.Combine(configdir, @$"{servicename}.plist"), service);
        }
    }
    static void StartWatching(string servicename)
    {
        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(1000 * 60);
                if (Stopped) return;

                ExecuteForOs(Windows, Linux, Mac);
            }
        })
        { IsBackground = true }.Start();


        void Windows()
        {
            // to hide a CA1416 warning
            if (!OperatingSystem.IsWindows()) return;

            using var ts = new TaskService();
            using var task = ts.GetTask(servicename);

            task.Run();
        }
        void Linux() => Launch(SystemctlExe, @$"--user start ${servicename}.service");
        void Mac() => throw new NotImplementedException();
    }

    public static void Start(bool useAdminRights, string? servicename = null)
    {
        Stopped = false;
        servicename ??= ServiceName;
        StartWatching(servicename);
        Initialize(useAdminRights, servicename);
        ExecuteForOs(Windows, Linux, Mac);


        void Windows()
        {
            using var ts = new TaskService();
            ts.Execute(servicename);
        }
        void Linux() => Launch(SystemctlExe, @$"--user enable --now {servicename}.timer");
        void Mac() => Launch(LaunchctlExe, @$"start {servicename}");
    }
    public static void Stop(string? servicename = null)
    {
        Stopped = true;
        servicename ??= ServiceName;
        ExecuteForOs(Windows, Linux, Mac);


        void Windows()
        {
            using var ts = new TaskService();
            ts.RootFolder.DeleteTask(servicename);
        }
        void Linux() => Launch(SystemctlExe, @$"--user disable --now {servicename}.timer");
        void Mac() => Launch(LaunchctlExe, @$"stop {servicename}");
    }


    static void Launch(string executable, string arguments) => Process.Start(executable, arguments).WaitForExit();
    static void ExecuteForOs(System.Action? windows, System.Action? linux, System.Action? mac)
    {
        if (OperatingSystem.IsWindows()) windows?.Invoke();
        else if (OperatingSystem.IsLinux()) linux?.Invoke();
        else if (OperatingSystem.IsMacOS()) mac?.Invoke();
    }
}
