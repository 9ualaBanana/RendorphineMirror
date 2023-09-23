using System.Security.Cryptography;
using System.Text;
using System.Web;
using NLog.Extensions.Logging;

namespace Common;

/// <summary> M+ specific api </summary>
public record Api : ApiBase
{
    public const string ServerUri = "https://tasks.microstock.plus";
    public const string TaskManagerEndpoint = $"{ServerUri}/rphtaskmgr";
    public static readonly Uri TaskLauncherEndpoint = new($"{ServerUri}/rphtasklauncher/");
    public const string ContentDBEndpoint = $"https://cdb.microstock.plus/contentdb";

    public static readonly HttpClient GlobalClient = new();
    public static readonly Api Default = new(GlobalClient);

    public Api(HttpClient client) : base(client, new NLogLoggerFactory().CreateLogger<Api>()) { }
    public Api(HttpClient client, ILogger<Api> logger) : base(client, logger) { }

    public static (string, string)[] AddSessionId(string sessionid, params (string, string)[] values)
    {
        if (values.Any(x => x.Item1 == "sessionid")) return values;
        return values.Append(("sessionid", sessionid)).ToArray();
    }
    public static (string, string)[] SignRequest(string key, params (string, string)[] values) => values.Append(("sign", CalculateSign(key, values))).ToArray();
    public static string CalculateSign(string key, params (string, string)[] values)
    {
        var content = string.Join('|', values.Select(x => x.Item2));
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }


    protected override bool NeedsToRetryRequest(OperationResult result)
    {
        // true only when http code is non-success (except 400) and there's no response json
        return result.Error is HttpError
        {
            IsSuccessStatusCode: false,
            StatusCode: not System.Net.HttpStatusCode.BadRequest,
            ErrorCode: null,
        };
    }
    public override async Task<OperationResult<JToken>> ResponseJsonToOpResult(HttpResponseMessage response, JToken? responseJson, string errorDetails, CancellationToken token)
    {
        async ValueTask<string> contentToString(object? content) =>
            content switch
            {
                (string, string)[] pcontent => string.Join('&', pcontent.Select(x => x.Item1 + "=" + HttpUtility.UrlDecode(x.Item2))),
                FormUrlEncodedContent c => HttpUtility.UrlDecode(await c.ReadAsStringAsync(token)),
                StringContent c => await c.ReadAsStringAsync(token),
                MultipartContent c => $"Multipart [{string.Join(", ", await Task.WhenAll(c.Select(async c => $"{{ {c.Headers.ContentDisposition?.Name ?? "<noname>"} : {await contentToString(c)} }}")))}]",
                { } => content.GetType().Name,
                _ => "<nocontent>",
            };


        var errcode = responseJson?["errorcode"]?.Value<int>();
        var errmsg = responseJson?["errormessage"]?.Value<string>();

        if (LogRequests)
        {
            var logerr = new HttpError(responseJson?.ToString(Formatting.None), response, errcode);
            Logger.LogTrace($"[{errorDetails}] [{response.RequestMessage?.Method.Method} {response.RequestMessage?.RequestUri} {{ {await contentToString(response.Content)} }}]: {logerr}");
        }

        var ok =
            response.IsSuccessStatusCode
            && responseJson?["ok"]?.Value<int>() is null or 1
            && errcode is null or 0
            && errmsg is null;

        if (ok) return new OperationResult<JToken>(OperationResult.Succ(), responseJson);
        return OperationResult.Err(new HttpError(errmsg, response, errcode));
    }

    public override HttpContent ToPostContent((string, string)[] values) => new FormUrlEncodedContent(values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)));
}
