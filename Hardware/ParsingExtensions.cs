using System.Management.Automation;

namespace Hardware;

internal static class ParsingExtensions
{
    internal static string Value(this string keyValuePair, bool removeUnits = false)
    {
        var value = keyValuePair.Split(':', StringSplitOptions.TrimEntries)[1];
        return removeUnits ? value.Split()[0] : value;
    }

    internal static T? Value<T>(this PSPropertyInfo? psPropertyInfo)
    {
        return (T?)psPropertyInfo?.Value;
    }
}
