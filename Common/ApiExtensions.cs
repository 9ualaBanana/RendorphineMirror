namespace Common;

public static class ApiExtensions
{
    public static async ValueTask<JToken> GetJsonIfSuccessfulAsync(this HttpResponseMessage response, string? errorDetails = null)
    {
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new JsonTextReader(new StreamReader(stream));
        var responseJson = JToken.Load(reader);
        var responseStatusCode = responseJson["ok"]?.Value<int>();
        if (responseStatusCode != 1)
        {
            if (responseJson["errormessage"]?.Value<string>() is { } errmsg)
                throw new HttpRequestException(errmsg);

            if (responseJson["errorcode"]?.Value<string>() is { } errcode)
                throw new HttpRequestException($"{errorDetails} Server responded with {errcode} error code");

            throw new HttpRequestException($"{errorDetails} Server responded with {responseStatusCode} status code");
        }

        return responseJson;
    }

}
