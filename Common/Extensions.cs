namespace Common
{
    public static class Extensions
    {
        public static string TrimLines(this string str) => string.Join(Environment.NewLine, str.Split('\n', StringSplitOptions.TrimEntries)).Trim();
        public static string TrimVerbatim(this string str)
        {
            var spt = str.Split(Environment.NewLine);
            var min = spt.Where(x => !string.IsNullOrWhiteSpace(x)).Min(x => x.TakeWhile(x => x == ' ').Count());
            return string.Join(Environment.NewLine, spt.Select(x => x.Length < min ? x : x.Substring(min).TrimEnd())).Trim();
        }
        public static string TrimVerbatimWithEmptyLines(this string str)
        {
            var spt = str.Split(Environment.NewLine);
            var min = spt.Where(x => !string.IsNullOrWhiteSpace(x)).Min(x => x.TakeWhile(x => x == ' ').Count());
            return string.Join(Environment.NewLine, spt.Select(x => x.Length < min ? x : x.Substring(min).TrimEnd()).Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        }
    }
}