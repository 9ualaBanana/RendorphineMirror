namespace Hardware;

internal static class LinuxParsingExtensions
{
    internal static string Value(this string keyValuePair, bool removeUnits = false)
    {
        var value = keyValuePair.Split(':', StringSplitOptions.TrimEntries)[1];
        return removeUnits ? value.Split()[0] : value;
    }
}
