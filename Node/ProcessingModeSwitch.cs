using System.Diagnostics;

namespace Node;

public class ProcessesingModeSwitch
{
    const int loopDelay = 30000;
    readonly string[] _blockingProcesses = new string[] { "3dsmax", "AfterFX" };
    readonly string[] _minerProcesses = new string[] { "NiceHashMiner", "app_nhm", "nicehashquickminer", "excavator", "nbminer", "miner", "CryptoDredge", "xmrig", "z-enemy", "nanominer", "TT - Miner" };
    readonly string[] _minerExecutables = new string[] { "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Programs\\NiceHash Miner\\NiceHashMiner.exe" };

    bool _wasBlocked;
    public bool IsBlocked
    {
        get
        {
            foreach (var blockingProcess in _blockingProcesses)
            {
                if (Process.GetProcessesByName(blockingProcess).Any()) return true;
            }
            return false;
        }
    }

    public async Task StartMonitoring()
    {
        while (true)
        {
            CheckMode();
            await Task.Delay(loopDelay);
        }
    }

    void CheckMode()
    {
        var isBlocked = IsBlocked;
        if (isBlocked != _wasBlocked)
        {
            _wasBlocked = isBlocked;
            if (isBlocked) KillMiners();
            else LaunchMiners();
        }
    }

    void KillMiners()
    {
        foreach (var minerProcess in _minerProcesses)
        {
            foreach (var process in Process.GetProcessesByName(minerProcess))
                process.Kill(true);
        }
    }

    void LaunchMiners()
    {
        foreach (var minerExecutable in _minerExecutables)
        {
            var existingMinerProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(minerExecutable));
            if (existingMinerProcesses.Any()) return;

            using var process = new Process()
            {
                StartInfo = new()
                {
                    WorkingDirectory = Path.GetDirectoryName(minerExecutable),
                    FileName = Path.GetFileName(minerExecutable),
                }
            };

            try { process.Start(); }
            catch (Exception e)
            {
                Console.WriteLine("[" + minerExecutable + "] " + e.Message);
            }
        }
    }
}
