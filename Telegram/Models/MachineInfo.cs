using Telegram.Services.Telegram.Updates.Commands;

namespace Telegram.Models;

public class MachineInfo : IEquatable<MachineInfo>
{
    public string UserId { get; set; } = null!;
    public string NodeName { get; set; } = null!;
    public string PCName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Guid { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string IP { get; init; } = null!;
    public string Port { get; init; } = null!;
    public string WebServerPort { get; init; } = null!;
    public HashSet<Plugin> InstalledPlugins { get; init; } = null!;

    public string BriefInfoMDv2 => $"*{NodeName}* {PCName} (v.*{Version}*) | *{IP}:{Port}* | *{IP}:{WebServerPort}/*";

    public MachineInfo WithVersionUpdatedTo(string version) { Version = version; return this; }

    public bool NameContainsAny(IEnumerable<string> names) =>
        names.Select(name => name.CaseInsensitive())
        .Any(name => NodeName.CaseInsensitive().Contains(name));

    #region EqualityContract
    public static bool operator ==(MachineInfo this_, MachineInfo other) => this_.Equals(other);
    public static bool operator !=(MachineInfo this_, MachineInfo other) => !this_.Equals(other);

    public override bool Equals(object? obj) => Equals(obj as MachineInfo);
    public bool Equals(MachineInfo? other) => NodeName.CaseInsensitive() == other?.NodeName.CaseInsensitive();
    public override int GetHashCode() => NodeName.ToLower().GetHashCode();
    #endregion
}
