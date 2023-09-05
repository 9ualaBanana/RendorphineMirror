using Google.Cloud.Vision.V1;

namespace ChatGptApi.Controllers;

[ApiController]
public class MainController : ControllerBase
{
    readonly OpenAICompleter OpenAICompleter;
    readonly ImageAnnotatorClient Client;
    readonly ILogger Logger;

    public MainController(OpenAICompleter openAICompleter, ImageAnnotatorClient client, ILogger<MainController> logger)
    {
        OpenAICompleter = openAICompleter;
        Client = client;
        Logger = logger;
    }

    record TKD(string Title, string Description, IReadOnlyCollection<string> Keywords);
    [HttpPost("generatetkd")]
    public async Task<JToken> GenerateTKD([FromQuery] string sessionid, [FromQuery] string taskid, [FromForm] IFormFile img)
    {
        if (!await TaskTypeChecker.IsTaskTypeValid(TaskAction.GenerateTitleKeywords, sessionid, taskid))
            return JsonApi.Error("no");

        using var stream = img.OpenReadStream();
        var image = await Image.FromStreamAsync(stream);

        // score and topicality always return the same value
        // https://issuetracker.google.com/issues/117855698
        var labels = await Client.DetectLabelsAsync(image, maxResults: 50);

        const float kwcutoff = .51f;
        var keywords = labels
            .Where(l => l.Score > kwcutoff)
            .Select(l => l.Description)
            .ToImmutableArray();

        var title = await OpenAICompleter.GenerateNewTitle(keywords);
        var description = await OpenAICompleter.GenerateNewDescription(title, keywords);

        Logger.LogInformation($"""
            For image {img.FileName}:
                Labels: {string.Join(", ", labels.Select(l => $"{(l.Score > kwcutoff ? "" : "*")}{(int) (l.Score * 100)}% {l.Description}"))}
                Title: "{title}"
                Description: "{description}"
            """);

        return JsonApi.Success(new TKD(title, description, keywords));
    }

    public record TK(string Title, IReadOnlyCollection<string> Keywords);
    [HttpGet("generatebettertk")]
    public async Task<JToken> GenerateBetterTK([FromQuery] string sessionid, [FromQuery] string taskid, [FromQuery] string title, [FromQuery] string keywords)
    {
        if (!await TaskTypeChecker.IsTaskTypeValid(TaskAction.GenerateTitleKeywords, sessionid, taskid))
            return JsonApi.Error("no");

        var keywordsarr = JsonConvert.DeserializeObject<string[]>(keywords).ThrowIfNull();

        var newtitle = await OpenAICompleter.GenerateBetterTitle(title, keywordsarr);
        var newkws = await OpenAICompleter.GenerateBetterKeywords(title, keywordsarr);

        return JsonApi.Success(new TK(newtitle, newkws));
    }
}
