using System.Globalization;

namespace Common;

public static class CultureExtensions
{
    public static string ToStringInvariant<T>(this T obj) where T : IFormattable =>
        obj.ToString(null, CultureInfo.InvariantCulture);

    public static bool ContainsOrdinal(this string str, string value) =>
        str.Contains(value, StringComparison.Ordinal);

    public static bool ContainsOrdinal(this ReadOnlySpan<char> str, ReadOnlySpan<char> value) =>
        str.Contains(value, StringComparison.Ordinal);
}
