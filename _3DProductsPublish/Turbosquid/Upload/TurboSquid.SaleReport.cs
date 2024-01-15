using System.Text.RegularExpressions;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record SaleReport(string Name, long ProductID, double Price, double Rate, double Royalty, DateTimeOffset Date, string OrderType, string Source, string Comment)
    {   
        /// <param name="record">JS Object initialization string.</param>
        public static SaleReport Parse(string record)
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


        internal class Uri
        {
            const int ID = 20;
            internal static System.Uri For(int month, int year)
                // `xsl` argument defines a format of the requested resource: 0 - webpage; 1 - .csv file.
                => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = "Report/Index.cfm", Query = $"report_id={ID}&xsl=0&theyear={year}&theMonth={month}" }.Uri;
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
