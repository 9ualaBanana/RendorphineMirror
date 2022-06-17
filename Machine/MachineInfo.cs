using Common;
using Machine.MessageBuilders;
using System.Net;
using System.Text;

namespace Machine;

public record MachineInfo
{
    public static string NodeName => Settings.NodeName!;
    readonly public static string UserName = Environment.UserName;
    readonly public static string PCName = Environment.MachineName;
    readonly public static string Version = Init.Version;
    public static async Task<IPAddress> GetPublicIPAsync()
    {
        try { return await PortForwarding.GetPublicIPAsync(); }
        catch (Exception) { return IPAddress.None; }
    }
    readonly public static string Port = PortForwarding.Port.ToString();
    public static async Task<string> GetBriefInfoAsync() => $"{NodeName} {PCName} (v.{Version}) | {await GetPublicIPAsync()}:{Port}";

    public static async Task<string> ToTelegramMessageAsync(bool verbose = false)
    {
        if (!verbose) return await GetBriefInfoAsync();

        if (OperatingSystem.IsWindows()) return WindowsHardwareInfoMessage.Build();
        throw new PlatformNotSupportedException();
    }

    public static async Task<DTO> AsDTOAsync() => new(NodeName, PCName, UserName, Version, (await GetPublicIPAsync()).ToString(), Port);


    public class DTO : IEquatable<DTO>
    {
        public string NodeName { get; set; }
        public string PCName { get; set; }
        public string UserName { get; set; }
        public string Version { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }

        public DTO() : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        {
        }

        public DTO(string nodeName, string pcName, string userName, string version, string ip, string port)
        {
            NodeName = nodeName;
            PCName = pcName;
            UserName = userName;
            Version = version;
            IP = ip;
            Port = port;
        }

        public string GetBriefInfoMDv2() => $"*{NodeName}* {PCName} (v.*{Version}*) | *{IP}:{Port}*";

        public string ToQueryString()
        {
            var queryStringBuilder = new StringBuilder();
            var properties = typeof(DTO).GetProperties();
            foreach (var property in properties)
            {
                queryStringBuilder.Append($"{property.Name}={property.GetValue(this)}&");
            }
            queryStringBuilder.Length--;    // Removes dangling query parameter separator ('&').
            return queryStringBuilder.ToString();
        }

        #region EqualityContract
        public override bool Equals(object? obj)
        {
            return Equals(obj as DTO);
        }

        public bool Equals(DTO? other)
        {
            return NodeName == other?.NodeName;
        }

        public override int GetHashCode()
        {
            return NodeName.GetHashCode();
        }
        #endregion
    }
}
