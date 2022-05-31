using Common;
using Hardware;
using ReepoBot.Services.Telegram;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Hardware;

public class HardwareInfoForwarder : WebhookEventHandler<HardwareInfo>
{
    public HardwareInfoForwarder(ILogger<HardwareInfoForwarder> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    public override async Task HandleAsync(HardwareInfo hardwareInfo)
    {
        foreach (var subscriber in Bot.Subscriptions)
        {
            await Bot.SendTextMessageAsync(
                subscriber, BuildHardwareInfoMessage(hardwareInfo),
                parseMode: ParseMode.MarkdownV2);
        }
    }

    static string BuildHardwareInfoMessage(HardwareInfo hardwareInfo)
    {
        var message = new StringBuilder();

        message.AppendLine($"*{Init.Version}*");

        message.AppendLine(GetCpuInfoMessage(hardwareInfo.CpuInfo));
        message.AppendLine(GetGpuInfoMessage(hardwareInfo.GpuInfo));
        message.AppendLine(GetRamInfoMessage(hardwareInfo.RamInfo));
        message.AppendLine(GetDiskInfoMessage(hardwareInfo.DiskInfo));


        return message.ToString();
    }

    static string GetCpuInfoMessage(List<CpuInfo> cpuInfoForAll)
    {
            var result = new StringBuilder();
            result.AppendLine("*CPU:*");
            result.AppendLine("---------".Sanitize());
            foreach (var cpuInfo in cpuInfoForAll)
            {
                result.AppendLine($"{cpuInfo.Name}  [ *{cpuInfo.CoreCount}* cores | *{cpuInfo.ThreadCount}* threads ]".Sanitize());
                result.AppendLine();
                result.AppendLine($"*Clock*: *{cpuInfo.CpuClockInfo.CurrentClock}* MHz / *{cpuInfo.CpuClockInfo.MaxClock}* MHz");
                result.AppendLine($"*Load*: *{cpuInfo.LoadPercentage}* %");
                result.AppendLine();
            }
            return result.ToString();
    }

    static string GetGpuInfoMessage(List<GpuInfo> gpuInfoForAll)
    {
        var result = new StringBuilder();
        result.AppendLine("*GPU:*");
        result.AppendLine("---------".Sanitize());
        foreach (var gpuInfo in gpuInfoForAll)
        {
            result.AppendLine($"{gpuInfo.Name}  [ *{gpuInfo.Memory.Used}* MB / *{gpuInfo.Memory.Total}* MB ]".Sanitize());
            result.AppendLine();
            result.AppendLine($"*Clocks:*");
            result.AppendLine($"\t*Core:* *{gpuInfo.GpuClockInfo.CurrentCoreClock}* MHz / *{gpuInfo.GpuClockInfo.MaxCoreClock}* MHz");
            result.AppendLine($"\t*Memory:* *{gpuInfo.GpuClockInfo.CurrentMemoryClock}* MHz / *{gpuInfo.GpuClockInfo.MaxMemoryClock}* MHz");
            result.AppendLine();
        }
        return result.ToString();
    }

    static string GetRamInfoMessage(List<RamInfo> ramInfoForAll)
    {
        var result = new StringBuilder();
        result.AppendLine("*RAM:*");
        result.AppendLine("---------".Sanitize());
        foreach (var ramInfo in ramInfoForAll)
        {
            result.AppendLine($"{ramInfo.DeviceLocator} [ *{ramInfo.Memory.Total}* MB | *{ramInfo.MemoryClock}* MHz ]".Sanitize());
            result.AppendLine();
        }
        return result.ToString();
    }

    static string GetDiskInfoMessage(List<DiskInfo> diskInfoForAll)
    {
        var result = new StringBuilder();
        result.AppendLine("*Disks:*");
        result.AppendLine("---------".Sanitize());
        foreach (var diskInfo in diskInfoForAll)
        {
            result.AppendLine($"{diskInfo.Caption}  [ *{diskInfo.StorageSpace.Used:.##}* GB / *{diskInfo.StorageSpace.Total:.##}* GB ]".Sanitize());
            result.AppendLine();
        }
        return result.ToString();
    }
}
