namespace ChatGptApi;

public class ElevenLabsApis
{
    public const string Endpoint = ElevenLabsApi.Endpoint;

    public string ApiKey { get; }
    public ElevenLabsApi Api { get; }

    public ElevenLabsApis(string apiKey, ElevenLabsApi api)
    {
        ApiKey = apiKey;
        Api = api;
    }


    HttpRequestMessage CreateGetRequest(string url, params (string, string)[] values) => new HttpRequestMessage(HttpMethod.Get, ApiBase.AppendQuery(url, values));
    HttpRequestMessage CreatePostRequest(string url, params (string, string)[] values) => CreatePostRequest(url, Api.ToPostContent(values));
    HttpRequestMessage CreatePostRequest(string url, JObject json) => CreatePostRequest(url, ElevenLabsApi.ToPostContent(json));
    HttpRequestMessage CreatePostRequest(string url, HttpContent content) => new HttpRequestMessage(HttpMethod.Post, url) { Content = content, };
    HttpRequestMessage AddAuthentication(HttpRequestMessage request)
    {
        request.Headers.Add("xi-api-key", ApiKey);
        return request;
    }


    public async Task<OperationResult<ImmutableArray<Voice>>> GetVoiceListAsync() =>
        await Api.ApiGet<ImmutableArray<Voice>>($"{Endpoint}/voices", "voices", "Getting voice list");

    public async Task<OperationResult> TextToSpeechAsync(string voiceid, string modelid, string text, string filename)
    {
        var data = new JObject()
        {
            ["text"] = text,
            ["model_id"] = modelid,
            ["voice_settings"] = new JObject()
            {
                ["stability"] = .5f,
                ["similarity_boost"] = .75f,
                ["style"] = 0,
                ["use_speaker_boost"] = true
            },
        };

        using var request = AddAuthentication(CreatePostRequest($"{Endpoint}/text-to-speech/{voiceid}", data));
        return await Api.ApiSendFile(request, filename, "Generating TTS");
    }


    public record Voice([property: JsonProperty("voice_id")] string VoiceId, string Name, [property: JsonProperty("high_quality_base_model_ids")] ImmutableArray<string> HighQualityBaseModelIds);
}
