using System.ComponentModel;
using System.Globalization;

namespace ReepoBot.Models;

[TypeConverter(typeof(GeoPointConverter))]
public record struct NodeInfo
{
    public string Name { get; }
    public string Version { get; }
    public string IP { get; }

    public NodeInfo(string name, string version, string ip)
    {
        Name = name;
        Version = version;
        IP = ip;
    }

    public static bool TryParse(string s, out NodeInfo? nodeInfo)
    {
        nodeInfo = default;

        var queryStringParameters = s.Split(',');
        if (queryStringParameters.Length != 3)
        {
            return false;
        }

        var name = queryStringParameters[0];
        var version = queryStringParameters[1];
        var ip = queryStringParameters[2];
        nodeInfo = new(name, version, ip);
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