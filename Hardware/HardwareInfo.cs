using Common;
using Hardware.MessageBuilders;
using System.Net;
using System.Text;

namespace Hardware;

public record HardwareInfo
{
    readonly public static string UserName = Environment.UserName;
    readonly public static string PCName = Environment.MachineName;
    readonly public static string Version = Init.Version;
    public static async Task<IPAddress> GetPublicIPAsync()
    {
        try { return await PortForwarding.GetPublicIPAsync(); }
        catch (Exception) { return IPAddress.None; }
    }
    readonly public static string Port = PortForwarding.Port.ToString();
    public static async Task<string> GetBriefInfoAsync() => $"{PCName} {UserName} (v.{Version}) | {await GetPublicIPAsync()}:{Port}";

    public static async Task<string> ToTelegramMessageAsync(bool verbose = false)
    {
        if (!verbose) return await GetBriefInfoAsync();

        if (OperatingSystem.IsWindows()) return WindowsHardwareInfoMessage.Build();
        throw new PlatformNotSupportedException();
    }

    public static async Task<DTO> AsDTOAsync() => new(PCName, UserName, Version, (await GetPublicIPAsync()).ToString(), Port);


    public record class DTO(string PCName, string UserName, string Version, string IP, string Port)
    {
        public string GetBriefInfoMDv2() => $"*{PCName}* {UserName} (v.*{Version}*) | *{IP}:{Port}*";

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
    }
}
