using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Transport.Download;

internal static class Respond
{
    readonly static JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static void Ok(HttpListenerResponse response, IEnumerable<KeyValuePair<string, object?>>? args = default)
    {
        response.Close(BuildResponse(args), false);
    }

    internal static void NotOk(HttpListenerResponse response, string errorMessage)
    {
        response.Close(BuildResponse(errorMessage), false);
    }

    static byte[] BuildResponse(IEnumerable<KeyValuePair<string, object?>>? args)
    {
        const int ok = 1;
        var response = ResponseTemplate(ok);
        if (args is not null)
        {
            foreach (var (key, value) in args) response.Add(key, value);
        }
        return JsonSerializer.SerializeToUtf8Bytes(response, _options);
    }


    static byte[] BuildResponse(string errorMessage) =>
        JsonSerializer.SerializeToUtf8Bytes(ResponseTemplate(ok: 0, errorMessage), _options);

    static Dictionary<string, object?> ResponseTemplate(int ok, string? errorMessage = null) =>
        new Dictionary<string, object?>()
        {
            ["ok"] = ok,
            ["errormessage"] = errorMessage
        };
}
