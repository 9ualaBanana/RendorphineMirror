using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pinger
{
    public static class SystemService
    {
        const string WindowsServiceExe = @"C:\Windows\System32\sc.exe";
        const string SystemctlExe = "/usr/bin/systemctl"; // TODO: what if no systemd
        const string LaunchctlExe = "/usr/bin/launchctl";

        const string ServiceName = "renderphinepinger";


        public static void Initialize(string nodeexe)
        {
            var pingerexe = typeof(SystemService).Assembly.Location;
            if (IsOs(OSPlatform.Windows)) pingerexe = Path.ChangeExtension(pingerexe, "exe");
            else pingerexe = Path.ChangeExtension(pingerexe, null);

            ExecuteForOs(Windows, Linux, Mac);


            void Windows()
            {
                // delete old service
                Start(WindowsServiceExe, @$"delete {ServiceName}");

                // create a windows service
                Start(WindowsServiceExe, @$"create {ServiceName} binPath=""\""{pingerexe}\"" \""{nodeexe}\""""");

                // make it restart after crash
                Start(WindowsServiceExe, @$"failure {ServiceName} reset=0 actions=restart/60000/restart/60000/run/1000");
            }
            void Linux()
            {
                var service = $@"
                    [Unit]
                    Description=Renderphine tracker

                    [Service]
                    Type=notify
                    Restart=on-failure
                    ExecStart=""{pingerexe}"" ""{nodeexe}""
                ";

                var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "systemd/user/");
                File.WriteAllText(Path.Combine(configdir, @$"{ServiceName}.service"), service);

                Start(SystemctlExe, @$"--user daemon-reload");
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
                        </array>
                        <key>KeepAlive</key>
                        <true/>
                    </dict>
                    </plist>
                ";

                var configdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/LaunchAgents");
                File.WriteAllText(Path.Combine(configdir, @$"{ServiceName}.plist"), service);
            }
        }

        public static void Start()
        {
            ExecuteForOs(Windows, Linux, Mac);


            void Windows() => Start(WindowsServiceExe, @$"start {ServiceName}");
            void Linux() => Start(SystemctlExe, @$"--user start {ServiceName}.service");
            void Mac() => Start(LaunchctlExe, @$"start {ServiceName}");
        }
        public static void Stop()
        {
            ExecuteForOs(Windows, Linux, Mac);


            void Windows() => Start(WindowsServiceExe, @$"stop {ServiceName}");
            void Linux() => Start(SystemctlExe, @$"--user stop {ServiceName}.service");
            void Mac() => Start(LaunchctlExe, @$"stop {ServiceName}");
        }


        static Process Start(string executable, string arguments) => Process.Start(executable, arguments);
        static void ExecuteForOs(Action? windows, Action? linux, Action? mac)
        {
            if (IsOs(OSPlatform.Windows)) windows?.Invoke();
            else if (IsOs(OSPlatform.Linux)) linux?.Invoke();
            else if (IsOs(OSPlatform.OSX)) mac?.Invoke();
        }
        static bool IsOs(OSPlatform os) => RuntimeInformation.IsOSPlatform(os);
    }
}