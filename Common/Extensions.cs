using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class Extensions
    {
        public const string Jpeg = ".jpeg";
        public const string Mp4 = ".mp4";

        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static T With<T>(this T t, Action<T> action)
        {
            action(t);
            return t;
        }

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

        public static void Consume(this Task task) => task.ContinueWith(t =>
        {
            if (t.Exception is not null)
            {
                _logger.Error(t.Exception.Message);
                throw t.Exception;
            }
        }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        public static T ThrowIfNull<T>([NotNull] this T? value, string? message = null)
        {
            if (value is null)
                throw new NullReferenceException(message ?? "Value is null");

            return value;
        }

        public static T WithProperty<T>(this T jobj, string key, JToken value) where T : JToken
        {
            jobj[key] = value;
            return jobj;
        }


        public static void CopyDirectory(string source, string destination)
        {
            source = Path.GetFullPath(source);
            destination = Path.GetFullPath(destination);

            Directory.GetDirectories(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => Directory.CreateDirectory(x.Replace(source, destination)));
            Directory.GetFiles(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => File.Copy(x, x.Replace(source, destination)));
        }

        public static string AsUnixTimestamp(this DateTime dateTime) => AsUnixTimestamp(new DateTimeOffset(dateTime));
        public static string AsUnixTimestamp(this DateTimeOffset dateTimeOffset) => dateTimeOffset.ToUnixTimeMilliseconds().ToString();
    }
}