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
        {
            var msg = respjson["error"]?["message"]?.Value<string>();
            if (msg?.Contains("Rate limit") == true)
            {
                try
                {
                    const string tryagain = "Please try again in ";
                    var start = msg.IndexOf(tryagain, StringComparison.Ordinal) + tryagain.Length;
                    var end = msg.IndexOf("s.", start, StringComparison.Ordinal);

                    var wait = double.Parse(msg.AsSpan(start, end - start));
                    await Task.Delay(TimeSpan.FromSeconds(wait));
                }
                catch
                {
                    await Task.Delay(5000);
                }

                return await SendRequest(crequest);
            }

            throw new Exception("Error in chat completion: " + msg);
        }

        return respjson.ToObject<ChatResult>().ThrowIfNull();
    }
}
