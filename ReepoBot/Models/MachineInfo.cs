using ReepoBot.Services.Telegram;
using System.Text;

namespace ReepoBot.Models;

public class MachineInfo : IEquatable<MachineInfo>
{
    public string NodeName { get; set; } = null!;
    public string PCName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string IP { get; init; } = null!;
    public string Port { get; init; } = null!;
    public HashSet<Plugin> InstalledPlugins { get; init; } = null!;

    public string BriefInfoMDv2 => $"*{NodeName}* {PCName} (v.*{Version}*) | *{IP}:{Port}*";

    public string InstalledPluginsAsText
    {
        get
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Plugins installed on {BriefInfoMDv2}");
            messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
            foreach (var groupedPlugins in InstalledPlugins.GroupBy(nodePlugin => nodePlugin.Type))
            {
                messageBuilder.AppendLine($"{Enum.GetName(groupedPlugins.Key)}");
                foreach (var plugin in groupedPlugins)
                {
                    messageBuilder
                        .AppendLine($"\tVersion: {plugin.Version}")
                        // Directory root is determined based on the OS where the executable is running
                        // so Windows path is broken when running on linux and vice versa.
                        .AppendLine($"\tPath: {plugin.Path[..3].Replace(@"\", @"\\")}");
                }
                messageBuilder.AppendLine();
            }
            return messageBuilder.ToString();
        }
    }

    public bool NameContainsAny(IEnumerable<string> names)
    {
        var lcNames = names.Select(name => name.ToLower());
        return lcNames.Any(lcName => NodeName.ToLower().Contains(lcName));
    }

    #region EqualityContract
    public static bool operator ==(MachineInfo this_, MachineInfo other)
    {
        return this_.Equals(other);
    }

    public static bool operator !=(MachineInfo this_, MachineInfo other)
    {
        return !this_.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MachineInfo);
    }

    public bool Equals(MachineInfo? other)
    {
        return NodeName.ToLower() == other?.NodeName.ToLower();
    }

    public override int GetHashCode()
    {
        return NodeName.ToLower().GetHashCode();
    }
    #endregion
}
