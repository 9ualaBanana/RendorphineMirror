using System.ComponentModel;
using System.Globalization;

namespace ReepoBot.Models;

[TypeConverter(typeof(GeoPointConverter))]
public record struct NodeInfo
{
    public string PCName { get; }
    public string UserName { get; }
    public string Version { get; set; }
    public string IP { get; }
    public string Port { get; }
    public string BriefInfoMDv2 => $"*{PCName}* {UserName} (v.*{Version}*) | *{IP}:{Port}*";

    public NodeInfo(string pcName, string userName, string version, string ip, string port)
    {
        PCName = pcName;
        UserName = userName;
        Version = version;
        IP = ip;
        Port = port;
    }

    public static bool TryParse(string s, out NodeInfo? nodeInfo)
    {
        nodeInfo = default;

        var queryStringParameters = s.Split(',');
        if (queryStringParameters.Length != 5)
        {
            return false;
        }

        var pcName = queryStringParameters[0];
        var userName = queryStringParameters[1];
        var version = queryStringParameters[2];
        var ip = queryStringParameters[3];
        var port = queryStringParameters[4];
        nodeInfo = new(pcName, userName, version, ip, port);
        return true;
    }
}

class GeoPointConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context,
        CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            if (NodeInfo.TryParse(stringValue, out NodeInfo? nodeInfo))
            {
                return nodeInfo;
            }
        }
        return base.ConvertFrom(context, culture, value);
    }
}