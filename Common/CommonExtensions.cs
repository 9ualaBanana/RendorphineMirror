using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class CommonExtensions
    {
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

        public static void Consume<T>(this ValueTask<T> task) => task.AsTask().Consume();
        public static void Consume<T>(this Task<T> task) => ((Task) task).Consume();
        public static void Consume(this ValueTask task) => task.AsTask().Consume();
        public static void Consume(this Task task) => task.ContinueWith(t =>
        {
            if (t.Exception is not null)
            {
                _logger.Error(t.Exception.Message);
                throw t.Exception;
            }
        }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        public static T ThrowIfValueNull<T>([NotNull] this T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? expression = null) where T : struct
        {
            if (value is null)
                throw new NullReferenceException(message ?? $"{expression ?? "Value"} is null");

            return value.Value;
        }
        public static T ThrowIfNull<T>([NotNull] this T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string? expression = null)
        {
            if (value is null)
                throw new NullReferenceException(message ?? $"{expression ?? "Value"} is null");

            return value;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class =>
            enumerable.Where(item => item is not null)!;

        public static T WithProperty<T>(this T jobj, string key, JToken value) where T : JToken
        {
            jobj[key] = value;
            return jobj;
        }

        public static void CopyDirectory(string source, string destination) => Directories.Copy(source, destination);
        public static void MergeDirectories(string source, string destination) => Directories.Merge(source, destination);

        public static void MakeExecutable(params string[] paths)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) return;

            var p = new ProcessStartInfo("/usr/bin/chmod")
            {
                ArgumentList = { "+x", "-R" },
                UseShellExecute = true,
            };
            foreach (var path in paths)
                p.ArgumentList.Add(path);

            Process.Start(p)!.WaitForExit();
        }

        public static string AsUnixTimestamp(this DateTime dateTime) => AsUnixTimestamp(new DateTimeOffset(dateTime));
        public static string AsUnixTimestamp(this DateTimeOffset dateTimeOffset) => dateTimeOffset.ToUnixTimeMilliseconds().ToString();
    }
}