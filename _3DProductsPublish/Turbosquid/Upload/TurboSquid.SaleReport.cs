using System.Text.RegularExpressions;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record SaleReport(string Name, long ProductID, double Price, double Rate, double Royalty, DateTimeOffset Date, string OrderType, string Source, string Comment)
    {   
        public Uri ProductPreview { get; internal set; }

        /// <param name="record">JS Object initialization string.</param>
        internal static SaleReport Parse(string record)
        {
            return new(
                record.Value("Name"),
                long.Parse(record.Value("ProductID")),
                double.Parse(Digit().Match(record.Value("Price")).ValueSpan),
                double.Parse(Digit().Match(record.Value("Rate")).ValueSpan),
                double.Parse(Digit().Match(record.Value("Royalty")).ValueSpan),
                DateTimeOffset.Parse(record.Value("Date").Split(["'", "\""], 3, default)[1]),
                record.Value("OrderType"),
                record.Value("Source"),
                record.Value("Comment"));
        }
        [GeneratedRegex(@"\d+(\.\d+)?", RegexOptions.Compiled)]
        private static partial Regex Digit();


        internal class Url
        {
            const int ID = 20;
            internal static Uri For(int month, int year)
                => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = "Report/Index.cfm", Query = $"report_id={ID}&xsl=0&theyear={year}&theMonth={month}" }.Uri;
            // `xsl` argument defines a format of the requested resource: 0 - webpage; 1 - .csv file.

            internal static Uri ForProductPreview(long id)
                => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = $"FullPreview/Index.cfm/ID/{id}" }.Uri;
        }
    }
}

static class StringExtensions
{
    internal static int EndIndexOf(this string _, string value)
        => _.IndexOf(value) is int index && index is not -1 ? index += value.Length : index;

    internal static string Value(this string _, string property)
    {
        var valueIndex = _.EndIndexOf($"{property}=");
        return _[valueIndex.._.IndexOf(';', startIndex: valueIndex)].Trim('"');
    }
}
