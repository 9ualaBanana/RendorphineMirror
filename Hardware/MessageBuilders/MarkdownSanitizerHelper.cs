namespace Hardware.MessageBuilders;

public static class MarkdownSanitizerHelper
{
    public static string Sanitize(this string unsanitizedString)
    {
        return unsanitizedString
            .Replace("|", @"\|")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("(", @"\(")
            .Replace(")", @"\)");
    }
}
