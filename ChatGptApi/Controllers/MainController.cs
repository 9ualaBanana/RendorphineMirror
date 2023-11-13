using ChatGptApi.OpenAiApi;
using Google.Cloud.Vision.V1;
using Node.Common.Models;

namespace ChatGptApi.Controllers;

[ApiController]
public class MainController : ControllerBase
{
    readonly OpenAICompleter OpenAICompleter;
    readonly ImageAnnotatorClient Client;
    readonly ElevenLabsApis ElevenLabsApi;
    readonly DataDirs Dirs;
    readonly ILogger Logger;

    public MainController(OpenAICompleter openAICompleter, ImageAnnotatorClient client, ElevenLabsApis elevenLabsApi, DataDirs dirs, ILogger<MainController> logger)
    {
        OpenAICompleter = openAICompleter;
        Client = client;
        ElevenLabsApi = elevenLabsApi;
        Dirs = dirs;
        Logger = logger;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GenerateTitleKeywordsSource
    {
        ChatGPT,
        VisionDenseCaptioning,
    }

    [HttpPost("generatetkd")]
    public async Task<JToken> GenerateTKD(
        [FromQuery] string sessionid,
        [FromQuery] string taskid,
        [FromForm] IFormFile img,
        [FromForm] string? model = null,
        [FromForm] string? titleprompt = null,
        [FromForm] string? kwprompt = null,
        [FromForm] string? prompt = null,
        [FromForm] GenerateTitleKeywordsSource source = GenerateTitleKeywordsSource.VisionDenseCaptioning,
        [FromForm] ChatRequest.ImageMessageContent.ImageDetail detail = ChatRequest.ImageMessageContent.ImageDetail.Low
    )
    {
        await TaskTypeChecker.ThrowIfTaskTypeNotValid(TaskAction.GenerateTitleKeywords, sessionid, taskid);

        if (source == GenerateTitleKeywordsSource.VisionDenseCaptioning)
            return JsonApi.Success(await GenerateTKVision(img, model, titleprompt, kwprompt));
        if (source == GenerateTitleKeywordsSource.ChatGPT)
        {
            try { return JsonApi.Success(await GenerateTKChatGpt(img, prompt, detail)); }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in chatgpt api, retrying");
                return JsonApi.Success(await GenerateTKVision(img, model, titleprompt, kwprompt));
            }
        }

        return JsonApi.Error("Unknown source");
    }


    async Task<TK> GenerateTKChatGpt(IFormFile img, string? prompt, ChatRequest.ImageMessageContent.ImageDetail detail)
    {
        using var imgstream = img.OpenReadStream();
        var imgbytes = new byte[img.Length];
        await imgstream.ReadExactlyAsync(imgbytes);
        var imgbase64 = Convert.ToBase64String(imgbytes);

        var tk = await OpenAICompleter.GenerateTKChatGpt(
            ChatRequest.ImageMessageContent.FromBase64(MimeTypes.GetMimeType(img.FileName), imgbase64, detail),
            prompt
        );

        Logger.LogInformation($"""
            For image {img.FileName}:
                Title: "{tk.Title}"
                Keywords: ["{string.Join(", ", tk.Keywords)}"]
            """);

        return tk;
    }
    async Task<TK> GenerateTKVision(IFormFile img, string? model, string? titleprompt, string? kwprompt)
    {
        using var imgstream = img.OpenReadStream();
        var image = await Image.FromStreamAsync(imgstream);

        // score and topicality always return the same value
        // https://issuetracker.google.com/issues/117855698
        var labels = await Client.DetectLabelsAsync(image, maxResults: 50);

        const float kwcutoff = .51f;
        var keywords = labels
            .Where(l => l.Score > kwcutoff)
            .Select(l => l.Description)
            .ToArray();

        var title = await OpenAICompleter.GenerateNewTitle(keywords, titleprompt, model);
        keywords = await OpenAICompleter.GenerateBetterKeywords(title, keywords, kwprompt, model);

        Logger.LogInformation($"""
            For image {img.FileName}:
                Labels: {string.Join(", ", labels.Select(l => $"{(l.Score > kwcutoff ? "" : "*")}{(int) (l.Score * 100)}% {l.Description}"))}
                Title: "{title}"
                Keywords: ["{string.Join(", ", keywords)}"]
            """);

        return new TK(title, keywords);
    }

    [HttpPost("generatebettertk")]
    public async Task<JToken> GenerateBetterTK(
        [FromQuery] string sessionid,
        [FromQuery] string taskid,
        [FromForm] string title,
        [FromForm] string keywords,
        [FromForm] string? model = null,
        [FromForm] string? titleprompt = null,
        [FromForm] string? kwprompt = null
    )
    {
        if (!await TaskTypeChecker.IsTaskTypeValid(TaskAction.GenerateTitleKeywords, sessionid, taskid))
            return JsonApi.Error("no");

        var keywordsarr = JsonConvert.DeserializeObject<string[]>(keywords).ThrowIfNull();

        var newtitle = await OpenAICompleter.GenerateBetterTitle(title, keywordsarr, titleprompt, model);
        var newkws = await OpenAICompleter.GenerateBetterKeywords(title, keywordsarr, kwprompt, model);

        return JsonApi.Success(new TK(newtitle, newkws));
    }


    [HttpGet("eleven/getvoices")]
    public async Task<JToken> GetVoices([FromQuery] string sessionid, [FromQuery] string taskid)
    {
        if (!await TaskTypeChecker.IsTaskTypeValid(TaskAction.GenerateAIVoice, sessionid, taskid))
            return JsonApi.Error("no");

        return JsonApi.JsonFromOpResult(await ElevenLabsApi.GetVoiceListAsync());
    }

    [HttpPost("eleven/tts")]
    public async Task<ActionResult> GenerateTextToSpeech([FromForm] string sessionid, [FromForm] string taskid, [FromForm] string text, [FromForm] string voiceid, [FromForm] string modelid)
    {
        if (!await TaskTypeChecker.IsTaskTypeValid(TaskAction.GenerateAIVoice, sessionid, taskid))
            return BadRequest(JsonApi.Error("no"));


        using var _ = Directories.DisposeDelete(Dirs.TempFile(), out var resultfile);
        var ttsresult = await ElevenLabsApi.TextToSpeechAsync(voiceid, modelid, text, resultfile);
        if (!ttsresult) return BadRequest(JsonApi.JsonFromOpResult(ttsresult));

        var bytes = await System.IO.File.ReadAllBytesAsync(resultfile);
        return File(bytes, "audio/mpeg");
    }
}
