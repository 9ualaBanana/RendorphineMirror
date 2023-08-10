using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;

namespace NodeToUI;

public static class SystemService
{
    const string SystemctlExe = "/usr/bin/systemctl";
    const string LaunchctlExe = "/usr/bin/launchctl";
    const string ServiceName = "renderfin_pinger";

    static void Initialize(string? servicename = null)
    {
        servicename ??= ServiceName;

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

            using var ts = new TaskService();

            var task = ts.NewTask();
            task.RegistrationInfo.Description = " pinger";
            task.Actions.Add(new ExecAction(pingerexe, workingDirectory: Directory.GetCurrentDirectory()));
            if (Initializer.UseAdminRights) task.Principal.RunLevel = TaskRunLevel.Highest;
            task.Settings.DisallowStartIfOnBatteries = false;
            task.Settings.StopIfGoingOnBatteries = false;
            task.Settings.Enabled = true;
            task.Settings.WakeToRun = true;
            task.Settings.AllowHardTerminate = true;
            task.Settings.ExecutionTimeLimit = TimeSpan.FromHours(3);

            // trigger immediately & then every minute forever
            task.Triggers.Add(repeated(new RegistrationTrigger()));
            if (Initializer.UseAdminRights) task.Triggers.Add(repeated(new BootTrigger()));

            var logon = new LogonTrigger();
            if (!Initializer.UseAdminRights)
                logon.UserId = WindowsIdentity.GetCurrent().Name;
            task.Triggers.Add(repeated(logon));

            // trigger on unhibernation
            //try { task.Triggers.Add(repeated(new EventTrigger("Microsoft-Windows-Diagnostics-Performance/Operational", "PowerTroubleshooter", 1))); }
            //catch { }
            try { task.Triggers.Add(repeated(new EventTrigger("System", "PowerTroubleshooter", 1))); }
            catch { }

            ts.RootFolder.RegisterTask(servicename, task.XmlText, createType: TaskCreation.CreateOrUpdate, logonType: TaskLogonType.InteractiveToken);


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
    public static void Start(string? servicename = null)
    {
        servicename ??= ServiceName;
        Initialize();
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


    static Process Launch(string executable, string arguments) => Process.Start(executable, arguments);
    static void ExecuteForOs(System.Action? windows, System.Action? linux, System.Action? mac)
    {
        if (OperatingSystem.IsWindows()) windows?.Invoke();
        else if (OperatingSystem.IsLinux()) linux?.Invoke();
        else if (OperatingSystem.IsMacOS()) mac?.Invoke();
    }
}