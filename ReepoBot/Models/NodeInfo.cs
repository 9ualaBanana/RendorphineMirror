using System.ComponentModel;
using System.Globalization;

namespace ReepoBot.Models;

[TypeConverter(typeof(GeoPointConverter))]
public class NodeInfo
{
    public string Name { get; }
    public string Version { get; }

    public NodeInfo(string name, string version)
    {
        Name = name;
        Version = version;
    }

    public static bool TryParse(string s, out NodeInfo? nodeInfo)
    {
        nodeInfo = default;

        var queryStringParameters = s.Split('&');
        if (queryStringParameters.Length != 2)
        {
            return false;
        }

        var nameParameter = queryStringParameters[0].ToLower();
        var versionParameter = queryStringParameters[1].ToLower();

        if (nameParameter.StartsWith("name=") && versionParameter.StartsWith("version="))
        {
            var name = nameParameter.Split('=')[1];
            var version = nameParameter.Split('=')[1];
            nodeInfo = new(name, version);
            return true;
        }
        return false;
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