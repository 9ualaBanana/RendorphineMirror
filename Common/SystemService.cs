using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.TaskScheduler;

namespace Common
{
    public static class SystemService
    {
        const string SystemctlExe = "/usr/bin/systemctl"; // TODO: what if no systemd
        const string LaunchctlExe = "/usr/bin/launchctl";

        const string ServiceName = "renderphinepinger";


        static void Initialize()
        {
            try { Stop(); }
            catch { }

            var nodeexe = FileList.GetNodeExe();
            var nodeuiexe = FileList.GetNodeUIExe();
            var pingerexe = FileList.GetPingerExe();

            var updaterexe = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
            if (updaterexe is null || !File.Exists(updaterexe)) updaterexe = FileList.GetUpdaterExe();

            MakeExecutable(pingerexe, updaterexe, nodeexe, nodeuiexe);
            ExecuteForOs(Windows, Linux, Mac);


            void Windows()
            {
                using var ts = new TaskService();

                var task = ts.NewTask();
                task.RegistrationInfo.Description = "Renderphine pinger";
                task.Actions.Add(new ExecAction(pingerexe, @$"""{nodeexe}"" ""{updaterexe}""", Directory.GetCurrentDirectory()));
                task.Principal.RunLevel = TaskRunLevel.Highest;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.Enabled = true;
                task.Settings.WakeToRun = true;

                // trigger immediately & then every minute forever
                task.Triggers.Add(repeated(new RegistrationTrigger()));
                task.Triggers.Add(repeated(new BootTrigger()));
                task.Triggers.Add(repeated(new LogonTrigger()));

                // trigger on unhibernation
                //try { task.Triggers.Add(repeated(new EventTrigger("Microsoft-Windows-Diagnostics-Performance/Operational", "PowerTroubleshooter", 1))); }
                //catch { }
                try { task.Triggers.Add(repeated(new EventTrigger("System", "PowerTroubleshooter", 1))); }
                catch { }

                ts.RootFolder.RegisterTask(ServiceName, task.XmlText, createType: TaskCreation.CreateOrUpdate, logonType: TaskLogonType.InteractiveToken);


                static T repeated<T>(T trigger) where T : Trigger, ITriggerDelay
                {
                    trigger.Delay = TimeSpan.FromMinutes(1);
                    trigger.Repetition = new RepetitionPattern(TimeSpan.FromMinutes(1), TimeSpan.Zero, false);

                    return trigger;
                }
            }
            void Linux()
            {
                var service = $@"
                    [Unit]
                    Description=Renderphine tracker

                    [Service]
                    Type=oneshot
                    KillMode=process
                    WorkingDirectory={Path.GetDirectoryName(updaterexe)}
                    ExecStart=""{pingerexe}"" ""{nodeexe}""  ""{updaterexe}""
                ";
                var timer = $@"
                    [Unit]
                    Description=Renderphine tracker

                    [Timer]
                    OnActiveSec=1min
                    OnUnitActiveSec=1min
                ";

                var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "systemd/user/");
                Directory.CreateDirectory(configdir);
                File.WriteAllText(Path.Combine(configdir, @$"{ServiceName}.service"), service);
                File.WriteAllText(Path.Combine(configdir, @$"{ServiceName}.timer"), timer);

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
                            <string>{nodeexe}</string>
                            <string>{updaterexe}</string>
                        </array>
                        <key>RunAtLoad</key>
                        <true/>
                        <key>StartInterval</key>
                        <integer>600</integer>
                    </dict>
                    </plist>
                ";

                var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/LaunchAgents");
                File.WriteAllText(Path.Combine(configdir, @$"{ServiceName}.plist"), service);
            }
        }
        public static void Start()
        {
            Initialize();
            ExecuteForOs(Windows, Linux, Mac);


            void Windows()
            {
                using var ts = new TaskService();
                ts.Execute(ServiceName);
            }
            void Linux() => Launch(SystemctlExe, @$"--user enable --now {ServiceName}.timer");
            void Mac() => Launch(LaunchctlExe, @$"start {ServiceName}");
        }
        public static void Stop()
        {
            ExecuteForOs(Windows, Linux, Mac);


            void Windows()
            {
                using var ts = new TaskService();
                ts.RootFolder.DeleteTask(ServiceName);
            }
            void Linux() => Launch(SystemctlExe, @$"--user stop {ServiceName}.timer");
            void Mac() => Launch(LaunchctlExe, @$"stop {ServiceName}");
        }


        static void MakeExecutable(params string[] paths)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) return;

            var p = new ProcessStartInfo("/usr/bin/chmod")
            {
                ArgumentList = { "+x" },
                UseShellExecute = true,
            };
            foreach (var path in paths)
                p.ArgumentList.Add(path);

            Process.Start(p);
        }
        static Process Launch(string executable, string arguments) => Process.Start(executable, arguments);
        static void ExecuteForOs(System.Action? windows, System.Action? linux, System.Action? mac)
        {
            if (IsOs(OSPlatform.Windows)) windows?.Invoke();
            else if (IsOs(OSPlatform.Linux)) linux?.Invoke();
            else if (IsOs(OSPlatform.OSX)) mac?.Invoke();
        }
        static bool IsOs(OSPlatform os) => RuntimeInformation.IsOSPlatform(os);
    }
}