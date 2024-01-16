using _3DProductsPublish.Turbosquid.Upload;
using System.Net;

namespace _3DProductsPublish.Turbosquid;

public class MPAnalytics
{
    const string Domain = "analytics.microstock.plus";

    public static async Task<MPAnalytics> LoginAsync(NetworkCredential credential, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler(); var client = new HttpClient(handler);

        var mpAuthRequest = new HttpRequestMessage(
            HttpMethod.Get,
            new UriBuilder { Scheme = "https", Host = "accounts.stocksubmitter.com", Path = "api/0.1/auth/native/authenticate" }.Uri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = credential.UserName,
                ["password"] = credential.Password,
                ["service"] = "renderphine"
            })
        };
        var mpAuth = JObject.Parse(await (await client.SendAsync(mpAuthRequest, cancellationToken)).Content.ReadAsStringAsync(cancellationToken));
        if (!(bool)mpAuth["isOk"]!)
            throw new HttpRequestException($"M+ response doesn't indicate success. ({(int)mpAuth["errorCode"]!})");

        var analyticsAuthRequest = new HttpRequestMessage(
            HttpMethod.Get,
            new UriBuilder { Scheme = "https", Host = Domain, Path = "getnewlogincode" }.Uri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["sid"] = (string)mpAuth["sid"]! })
        };
        var analyticsAuth = JObject.Parse(await (await client.SendAsync(analyticsAuthRequest, cancellationToken)).Content.ReadAsStringAsync(cancellationToken));
        if ((string?)analyticsAuth["logincode"] is string logincode)
            handler.CookieContainer.Add(new Cookie(nameof(logincode), logincode, "/", Domain));
        else throw new HttpRequestException($"{nameof(logincode)} is missing.");

        return new(client);
    }
    MPAnalytics(HttpClient client)
    { _client = client; }
    readonly HttpClient _client;

    public async Task SendAsync(IAsyncEnumerable<TurboSquid.SaleReports_.MonthlyScan> scans, CancellationToken cancellationToken)
    { await foreach (var scan in scans) await SendAsync(scan, cancellationToken); }
    public async Task SendAsync(TurboSquid.SaleReports_.MonthlyScan scan, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new UriBuilder { Scheme = "https", Host = Domain, Path = "storedata" }.Uri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["stockid"] = "turbosquid",
                ["olduntil"] = scan.TimePeriod.Start.ToString(),
                ["until"] = scan.TimePeriod.End.ToString(),
                ["data"] = JsonConvert.SerializeObject(
                    scan.SaleReports.Select(_ => new
                    {
                        id = _.ProductID.ToString(),
                        fname = _.Name,
                        ctype = ContentType.model,
                        preview = _.ProductPreview.AbsoluteUri,
                        title = _.Name,
                        date = _.Date.ToUnixTimeMilliseconds(),
                        type = SaleType.ondemand,
                        amount = _.Price
                    }))
            })
        };
        (await _client.SendAsync(request, cancellationToken)).EnsureSuccessStatusCode();
    }


    static class SaleType
    {
        internal const string subscription = nameof(subscription);
        internal const string ondemand = nameof(ondemand);
        internal const string enhanced = nameof(enhanced);
        internal const string referral = nameof(referral);
        internal const string partner = nameof(partner);
        internal const string database = nameof(database);
        internal const string other = nameof(other);
    }

    static class ContentType
    {
        internal const string model = nameof(model);
        internal const string raster = nameof(raster);
        internal const string vector = nameof(vector);
        internal const string video = nameof(video);
        internal const string audio = nameof(audio);
        internal const string other = nameof(other);
        internal const string unknown = nameof(unknown);
    }
}
