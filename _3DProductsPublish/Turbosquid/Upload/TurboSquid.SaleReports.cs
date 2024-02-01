using _3DProductsPublish.Turbosquid.Api;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial class SaleReports_
    {
        internal static async Task<SaleReports_> LoginAsync(TurboSquid turbosquid, CancellationToken cancellationToken)
        {
            var report = (await turbosquid._noAutoRedirectHttpClient.GetAsync(SaleReports_.Url.Index, cancellationToken)).SetCookies(turbosquid.Handler);
            report = (await report._FollowRedirectWith(turbosquid._noAutoRedirectHttpClient, cancellationToken)).SetCookies(turbosquid.Handler);    // Redirects to https://www.turbosquid.com/Login/Index.cfm?stgRU=https%3A%2F%2Fwww.turbosquid.com%2FReport%2FIndex.cfm%3Freport_id%3D20
            // Contains only _keymaster_session cookie which doesn't have Expires property.
            report = (await report._FollowRedirectWith(turbosquid._noAutoRedirectHttpClient, cancellationToken)).SetCookies(turbosquid.Handler);    // Redirects to https://auth.turbosquid.com/oauth/authorize?client_id=2c781a9f16cbd4fded77cf7f47db1927b85a5463185769bcb970cfdfe7463a0c&state=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MDUyNDY2NjguNSwic3ViIjoiUmVwb3J0L0luZGV4LmNmbSIsImV4cCI6MTcwNTI0NjY5OC41LCJqdGkiOiJDRDFCOUEwNi05RDcyLTQxNEUtOUI5NzNBMDVCMkMzMTY5RCJ9.3xeC5SohQT665dLD53L-UWAbT-zR_5a6ETGzzL_B9Aw&response_type=code&redirect_uri=https://www.turbosquid.com/Login/Keymaster.cfm?endpoint=callback&scope=id%20email%20roles%20device
            report = (await report._FollowRedirectWith(turbosquid._noAutoRedirectHttpClient, cancellationToken)).SetCookies(turbosquid.Handler);    // Redirects to https://www.turbosquid.com/Login/Keymaster.cfm?endpoint=callback&code=e5e6d7dadca042c277a24a5688de356ea4e1c6aef298723ce5201803e537445c&state=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MDUyNDY2NjguNSwic3ViIjoiUmVwb3J0L0luZGV4LmNmbSIsImV4cCI6MTcwNTI0NjY5OC41LCJqdGkiOiJDRDFCOUEwNi05RDcyLTQxNEUtOUI5NzNBMDVCMkMzMTY5RCJ9.3xeC5SohQT665dLD53L-UWAbT-zR_5a6ETGzzL_B9Aw
            await report._FollowRedirectWith(turbosquid, cancellationToken); // Final redirect to https://www.turbosquid.com/Report/Index.cfm?report_id=20
            return new(turbosquid);
        }
        SaleReports_(TurboSquid turbosquid)
        { _turbosquid = turbosquid; }
        readonly TurboSquid _turbosquid;

        public partial record MonthlyScan(ImmutableArray<SaleReport> SaleReports, ScanTimePeriodEntity TimePeriod)
        { public record TimePeriod_(long Start, long End); }
        public async IAsyncEnumerable<MonthlyScan> ScanAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var salereports = new DbContext();

            var user = await GetUserAsyncWith(_turbosquid.Credential.UserName);

            var current = DateTimeOffset.UtcNow;
            var olduntil = user.ScanPeriods.LastOrDefault()?.End ?? 0;
            long until;

            var year = DateTimeOffset.FromUnixTimeMilliseconds(olduntil).Year;
            int minYear; if (year < (minYear = 2001)) year = minYear;
            
            for (; year <= current.Year; year++)
                for (int month = DateTimeOffset.FromUnixTimeMilliseconds(olduntil).Month is int scannedMonth && scannedMonth is not 12 ? scannedMonth + 1 : 1;
                    month <= (year == current.Year ? current.Month : 12); month++)
                {
                    until = (year == current.Year && month == current.Month ?
                        DateTimeOffset.UtcNow : new DateTimeOffset(new(year, month, DateTime.DaysInMonth(year, month))))
                        .ToUnixTimeMilliseconds();

                    var scan = new MonthlyScan((await ScanAsync(year, month, cancellationToken)).ToImmutableArray(),
                        new(olduntil, until) { User = user });
                    salereports.Add(scan.TimePeriod);

                    olduntil = scan.TimePeriod.End;

                    yield return scan;
                }


            async ValueTask<UserEntity> GetUserAsyncWith(string username)
            {
                var user = await salereports.TurboSquid.FindAsync(username, cancellationToken);
                if (user is null)
                {
                    user = (await salereports.TurboSquid.AddAsync(new(username), cancellationToken)).Entity;
                    await salereports.SaveChangesAsync(cancellationToken);
                }
                return user;
            }
        }

        async Task<IEnumerable<SaleReport>> ScanAsync(int year, int month, CancellationToken cancellationToken)
        {
            var webpage = await _turbosquid.GetStringAsync(SaleReports_.Url.For(month, year), cancellationToken);
            var recordDefinitions = webpage[webpage.EndIndexOf("var row=new Object();")..webpage.IndexOf("responseSchema.push")]
                .Split(["row=new Object();", "colArr.push(row);"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var records = recordDefinitions.Select(SaleReport.Parse).ToArray();
            // Avoid extra preview requests for the records describing the same product (they have the same preview cause refer to the same product with the same product ID).
            foreach (var record in records)
                try { record.ProductPreview = await RequestProductPreviewAsync(record.ProductID); } catch { }
                
            return records;


            async Task<Uri> RequestProductPreviewAsync(long id)
            {
                var productPreviewWebpage = await
                    (await _turbosquid.GetAsync(SaleReports_.Url.ForProductPreview(id), cancellationToken))
                    .EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken);
                var previewIndex = productPreviewWebpage.EndIndexOf("data-src=\"");
                return new(productPreviewWebpage[previewIndex..productPreviewWebpage.IndexOf('"', previewIndex)]);
            }
        }


        internal class Url
        {
            const int ID = 20;
            internal static Uri Index => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = "Report/Index.cfm", Query = $"report_id={ID}" }.Uri;
            internal static Uri For(int month, int year)
                => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = "Report/Index.cfm", Query = $"report_id={ID}&xsl=0&theyear={year}&theMonth={month}" }.Uri;
            // `xsl` argument defines a format of the requested resource: 0 - webpage; 1 - .csv file.

            internal static Uri ForProductPreview(long id)
                => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = $"FullPreview/{id}" }.Uri;
        }
    }


    public partial record SaleReport(string Name, long ProductID, double Price, double Rate, double Royalty, DateTimeOffset Date, string OrderType, string Source, string Comment)
    {   
        // TODO: Statically ensure the proper initialization.
        public Uri? ProductPreview { get; internal set; }

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

    internal static string? JsonValue(this string _, string property)
    {
        var valueIndex = _.EndIndexOf($@"""{property}"":");
        var value = _[valueIndex.._.IndexOf(',', startIndex: valueIndex)].Trim('"');
        return value == "null" ? null : value;
    }
}
