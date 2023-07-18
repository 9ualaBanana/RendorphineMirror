namespace Common;

public static class Extensions
{
    public static bool ContainsOrdinal(this string str, string value) =>
        str.Contains(value, StringComparison.Ordinal);

    public static bool ContainsOrdinal(this ReadOnlySpan<char> str, ReadOnlySpan<char> value) =>
        str.Contains(value, StringComparison.Ordinal);
}
