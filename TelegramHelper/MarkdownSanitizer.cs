namespace TelegramHelper;

public static class MarkdownSanitizer
{
    public static string Sanitize(this string unsanitizedString)
    {
        return unsanitizedString
            .Replace("|", @"\|")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("_", @"\_")
            .Replace("(", @"\(")
            .Replace(")", @"\)");
    }
}