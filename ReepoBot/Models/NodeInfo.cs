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
    public string BriefInfoMDv2 => $"*{PCName}* {UserName} (v.*{Version}*) | *{IP}*";

    public NodeInfo(string userName, string pcName, string version, string ip)
    {
        PCName = pcName;
        UserName = userName;
        Version = version;
        IP = ip;
    }

    public static bool TryParse(string s, out NodeInfo? nodeInfo)
    {
        nodeInfo = default;

        var queryStringParameters = s.Split(',');
        if (queryStringParameters.Length != 4)
        {
            return false;
        }

        var pcName = queryStringParameters[0];
        var userName = queryStringParameters[1];
        var version = queryStringParameters[2];
        var ip = queryStringParameters[3];
        nodeInfo = new(pcName, userName, version, ip);
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