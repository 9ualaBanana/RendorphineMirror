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


    public override async Task<OperationResult> ResponseToResult(HttpResponseMessage response, JToken? responseJson, string errorDetails, CancellationToken token)
    {
        await LogRequest(response, responseJson, errorDetails);

        var errcode = responseJson?["errorcode"]?.Value<int>();
        var errmsg = responseJson?["errormessage"]?.Value<string>();


        var ok =
            response.IsSuccessStatusCode
            && responseJson?["ok"]?.Value<int>() is null or 1
            && errcode is null or 0
            && errmsg is null;

        if (ok) return OperationResult.Succ();
        return OperationResult.Err(new HttpError(errmsg, response, errcode));
    }

    public override HttpContent ToPostContent((string, string)[] values) => new FormUrlEncodedContent(values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)));
}
