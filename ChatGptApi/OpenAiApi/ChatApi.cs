namespace ChatGptApi.OpenAiApi;

public class ChatApi
{
    readonly HttpClient HttpClient;

    public ChatApi(string apiKey) =>
        HttpClient = new HttpClient() { DefaultRequestHeaders = { Authorization = new("Bearer", apiKey) } };

    public async Task<ChatResult> SendRequest(ChatRequest crequest)
    {
        using var reqcontent = new StringContent(JsonConvert.SerializeObject(crequest, JsonSettings.LowercaseIgnoreNull)) { Headers = { ContentType = new("application/json", "utf-8") } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions") { Content = reqcontent, };

        using var response = await HttpClient.SendAsync(request);
        using var resp = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync()));
        var respjson = await JObject.LoadAsync(resp);

        if (respjson.ContainsKey("error"))
            throw new Exception("Error in chat completion: " + respjson["error"]?["message"]?.Value<string>());

        return respjson.ToObject<ChatResult>().ThrowIfNull();
    }
}
