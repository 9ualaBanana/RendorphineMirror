using System.ComponentModel;
using System.Globalization;

namespace ReepoBot.Models;

[TypeConverter(typeof(GeoPointConverter))]
public record struct NodeInfo
{
    public string UserName { get; }
    public string PCName { get; }
    public string Version { get; }
    public string IP { get; }

    public NodeInfo(string userName, string pcName, string version, string ip)
    {
        UserName = userName;
        PCName = pcName;
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

        var userName = queryStringParameters[0];
        var pcName = queryStringParameters[1];
        var version = queryStringParameters[2];
        var ip = queryStringParameters[3];
        nodeInfo = new(userName, pcName, version, ip);
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