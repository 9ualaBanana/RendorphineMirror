using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.TaskScheduler;

namespace Pinger
{
    public static class SystemService
    {
        const string WindowsServiceExe = @"C:\Windows\System32\sc.exe";
        const string SystemctlExe = "/usr/bin/systemctl"; // TODO: what if no systemd
        const string LaunchctlExe = "/usr/bin/launchctl";

        const string ServiceName = "renderphinepinger";


        static void Initialize(string nodeexe, string updaterexe)
        {
            nodeexe = Path.GetFullPath(nodeexe);
            updaterexe = Path.GetFullPath(updaterexe);

            Stop();

            var pingerexe = typeof(SystemService).Assembly.Location;
            if (IsOs(OSPlatform.Windows)) pingerexe = Path.ChangeExtension(pingerexe, "exe");
            else
            {
                pingerexe = Path.ChangeExtension(pingerexe, null);
                MakeExecutable(pingerexe);
                MakeExecutable(nodeexe);
            }

            ExecuteForOs(Windows, Linux, Mac);


            void Windows()
            {
                using var ts = new TaskService();

                var task = ts.NewTask();
                task.RegistrationInfo.Description = "Renderphine pinger";
                task.Actions.Add(new ExecAction(pingerexe, @$"""{nodeexe}"" ""{updaterexe}""", Directory.GetCurrentDirectory()));

                // trigger immediately & then every minute forever
                var trigger = new RegistrationTrigger();
                trigger.Repetition = new RepetitionPattern(TimeSpan.FromMinutes(1), TimeSpan.Zero, false);
                task.Triggers.Add(trigger);

                ts.RootFolder.RegisterTask(ServiceName, task.XmlText, createType: TaskCreation.CreateOrUpdate, logonType: TaskLogonType.S4U);
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
                        <key>StartInterval</key>
                        <integer>600</integer>
                    </dict>
                    </plist>
                ";

                var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/LaunchAgents");
                File.WriteAllText(Path.Combine(configdir, @$"{ServiceName}.plist"), service);
            }
        }
        public static void Start(string nodeexe, string updaterexe)
        {
            Initialize(nodeexe, updaterexe);
            ExecuteForOs(Windows, Linux, Mac);


            void Windows()
            {
                using var ts = new TaskService();
                ts.Execute(ServiceName);
            }
            void Linux() => Launch(SystemctlExe, @$"--user start {ServiceName}.timer");
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


        static void MakeExecutable(string path)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) return;

            Process.Start(new ProcessStartInfo("/usr/bin/chmod")
            {
                ArgumentList = { "+x", path },
                UseShellExecute = true,
            });
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