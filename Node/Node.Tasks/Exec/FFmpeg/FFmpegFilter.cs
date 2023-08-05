using System.Runtime.CompilerServices;
using System.Text;

namespace Node.Tasks.Exec.FFmpeg;

public static class FFmpegFilter
{
    public class FilterList : MultiList<Block>
    {
        public string Build() => ToString();
        public override string ToString() => string.Join(',', this);
    }

    /// <summary>
    /// <code>
    /// [txt]
    ///     format=rgba,
    ///     pad= x=50 : w=iw+50 : color=#00000000
    /// [txt];
    /// </code>
    /// </summary>
    public class Block : MultiList<Filter>
    {
        readonly ImmutableArray<string> InputStreams, OutputStreams;

        public Block(ImmutableArray<string> input, ImmutableArray<string> output)
        {
            InputStreams = input;
            OutputStreams = output;
        }

        public string Build() => ToString();
        public override string ToString() => $"{WrapStreams(InputStreams)}{string.Join(',', this.Select(i => i.Build()))}{WrapStreams(OutputStreams)}";

        // [vid][wtr]
        static string WrapStreams(ImmutableArray<string> streams) => string.Join("", streams.Select(WrapStream));

        // [out]
        static string WrapStream(string stream) => $"[{stream}]";
    }

    /// <summary>
    /// <code>
    /// overlay= x=1 : y=2
    /// </code>
    /// </summary>
    public class Filter
    {
        readonly string Name;
        readonly Dictionary<string, string> Values = new();
        readonly List<string> FirstValues = new();

        public Filter(string name) => Name = name;


        /// <summary> Adds non-keyed value </summary>
        public Filter Add(string value)
        {
            FirstValues.Add(value);
            return this;
        }

        /// <inheritdoc cref="Add(string, string)"/>
        public Filter Add(FilterInterpolatedStringHandler value) => Add(value.ToString());

        /// <inheritdoc cref="Add(string, string)"/>
        public Filter Add<T>(T value) where T : IFormattable => Add($"{value}");

        /// <summary> Sets the value of <paramref name="key"/> to <paramref name="value"/> </summary>
        public Filter Set(string key, string value)
        {
            Values[key] = value;
            return this;
        }

        /// <inheritdoc cref="Set(string, string)"/>
        public Filter Set(string key, FilterInterpolatedStringHandler value) => Set(key, value.ToString());

        /// <inheritdoc cref="Set(string, string)"/>
        public Filter Set<T>(string key, T value) where T : IFormattable => Set(key, $"{value}");

        public string Build() => ToString();
        public override string ToString() => $"{Name}= {string.Join(':', FirstValues)} {((FirstValues.Count == 0 || Values.Count == 0) ? null : ":")} {string.Join(':', Values.Select(k => $"{k.Key}={k.Value}"))}";


        /// <summary>
        /// String interpolation handler to automatically format anything formattable using invariant culture (12345.678)
        /// </summary>
        [InterpolatedStringHandler]
        public readonly ref struct FilterInterpolatedStringHandler
        {
            readonly StringBuilder Builder = new();

            public FilterInterpolatedStringHandler(int literalLength, int _) =>
                Builder = new StringBuilder(literalLength);

            public void AppendLiteral(string s) => Builder.Append(s);

            public void AppendFormatted<T>(T number) where T : IFormattable =>
                AppendLiteral(number.ToString(null, CultureInfo.InvariantCulture));

            public void AppendFormatted(string str) => AppendLiteral(str);


            public override string ToString() => Builder.ToString();
        }
    }
}
