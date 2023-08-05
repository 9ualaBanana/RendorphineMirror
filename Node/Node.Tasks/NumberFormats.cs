namespace Node.Tasks;

public static class NumberFormats
{
    /// <summary>
    /// Number format with no number group separator and decimal limit set to 2.
    /// Example: 1212.34
    /// </summary>
    public static readonly NumberFormatInfo Normal = new()
    {
        NumberDecimalDigits = 2,
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = string.Empty,
    };

    /// <summary>
    /// Same as <see cref="Normal"/> but with decimal limit set to 10.
    /// Example: 1212.3456789012
    /// </summary>
    public static readonly NumberFormatInfo NoDecimalLimit = new()
    {
        NumberDecimalDigits = 10,
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = string.Empty,
    };
}
