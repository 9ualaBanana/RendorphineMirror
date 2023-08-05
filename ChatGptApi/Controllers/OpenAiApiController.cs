using Node.Tasks.Models;
using NodeCommon;
using NodeCommon.Tasks;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;

namespace ChatGptApi.Controllers;

[ApiController]
[Route("openai")]
public class OpenAiApiController : ControllerBase
{
    readonly ILogger Logger;
    readonly OpenAIAPI Api;

    public OpenAiApiController(ILogger<OpenAiApiController> logger, IConfiguration config)
    {
        Logger = logger;

        var apikey = config.GetValue<string>("openai_apikey");
        Api = new OpenAIAPI(apikey);
    }

    [HttpPost("generatetitlekeywords")]
    public async Task<JToken> GenerateTitleKeywords([FromForm] GenerateTitleKeywordsModel model)
    {
        // TODO: maybe handle empty keywords or title with a prompt like "generate a title" instead of "improve"

        var (sessionid, taskid, title, _) = model;
        var keywords = JsonConvert.DeserializeObject<ImmutableArray<string>>(model.Keywords);

        if (!await IsTaskTypeValid(sessionid, taskid))
            return JsonApi.Error("no");


        var newkeywords = new HashSet<string>();
        const int kwmaxtries = 3;
        var kwtries = 0;
        for (; kwtries < kwmaxtries; kwtries++)
        {
            if (newkeywords.Count >= 25)
                break;

            newkeywords.UnionWith(await GenerateKeywords(title, keywords));
        }

        var newtitle = string.Empty;
        const int titlemaxtries = 3;
        var titletries = 0;
        for (; titletries < titlemaxtries; titletries++)
        {
            if (newtitle.Length > 10)
                break;

            newtitle = await GenerateTitle(title, keywords);
        }

        Logger.LogInformation($"""
            Generated (k{kwtries}/{kwmaxtries} + t{titletries}/{titlemaxtries}):
                oldtitle='{title}'
                oldkeywords='{string.Join(", ", keywords)}'
                newtitle='{newtitle}'
                newkeywords='{string.Join(", ", newkeywords)}'
            """);


        var result = new
        {
            title = string.IsNullOrWhiteSpace(newtitle) ? title : newtitle,
            keywords = newkeywords.Concat(keywords).Take(50)
        };

        return JsonApi.Success(result);
    }

    const string GenerationExampleOldTitle = "Orange fruit on blue background. Ice on stick.";
    const string GenerationExampleNewTitle = "Orange on blue wooden background. Summer refreshing ices on stick.";
    const string GenerationExampleOldKeywords = "fruit, cool, tasty, refreshment, refreshing, orange, summer, wood, blue, fresh, fruity, flavored, dessert, freshness, homemade, juicy, flavor, stick, lollies, ice, lolly, plate, snack, wooden, table, white, citrus, healthy, pop";
    const string GenerationExampleNewKeywords = "sorbet, freeze, background, fruit, cool, tasty, refreshment, refreshing, orange, summer, wood, blue, fresh, fruity, flavored, natural, iced, cream, organic, gourmet, dessert, freshness, homemade, juicy, flavor, stick, lollies, ice, lolly, plate, snack, wooden, table, white, citrus, lollipop, icecream, cold, frozen, juice, food, sweet, healthy, pop";

    async Task<string> GenerateTitle(string title, IEnumerable<string> keywords)
    {
        const string header = "Improved title for better discoverability and sellability of a stock image:";

        var prompt = $"""
            {header}
            Old title:
            {GenerationExampleOldTitle}
            Keywords:
            {GenerationExampleOldKeywords}
            New title:
            {GenerationExampleNewTitle}

            {header}
            Old title:
            {title.Replace("\n", "").Replace("\"", "_").Replace("'", "_")}
            Keywords:
            {string.Join(", ", keywords)}
            New title:

            """;

        var completion = await Complete(prompt, max_tokens: 100);
        return completion.Trim();
    }
    async Task<IEnumerable<string>> GenerateKeywords(string title, IEnumerable<string> keywords)
    {
        const string header = "Improved keywords for better discoverability and sellability of a stock image:";

        var prompt = $"""
            {header}
            Title:
            {GenerationExampleOldTitle}
            Old keywords:
            {GenerationExampleOldKeywords}
            New keywords:
            {GenerationExampleNewKeywords}

            {header}
            Title:
            {title.Replace("\n", "").Replace("\"", "_").Replace("'", "_")}
            Old keywords:
            {string.Join(", ", keywords)}
            New keywords:

            """;

        var completion = await Complete(prompt, max_tokens: 300);
        return completion
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(k => !k.Contains(':', StringComparison.Ordinal))
            .Select(k => k.ToLowerInvariant())
            .Select(k => k.StartsWith("and ", StringComparison.Ordinal) ? k["and ".Length..] : k);
    }

    async Task<string> Complete(string prompt, Model? model = null, int max_tokens = 1000)
    {
        var request = new CompletionRequest(prompt, model: model ?? Model.CurieText, max_tokens: max_tokens, numOutputs: 1, stopSequences: "\n");
        var response = await Api.Completions.CreateCompletionAsync(request);
        var responsetext = response.Completions[0].Text;

        // æ for indicating AI response
        Logger.LogInformation($"Responded with:\n    {prompt.Replace("\n", "\n    ")}æ{responsetext}");
        return responsetext;
    }



    /*[HttpPost("complete")]
    public async Task<JObject> Complete(
        [FromForm] CompleteModel model
    )
    {
        var (sessionid, taskid, prompt) = model;

        if (!await IsTaskTypeValid(sessionid, taskid))
            return JsonApi.Error("no");

        var request = new CompletionRequest(prompt, model: Model.AdaText, max_tokens: 500);
        var completion = await Api.Completions.CreateAndFormatCompletion(request);

        return JsonApi.Success(completion);
    }*/


    static async Task<bool> IsTaskTypeValid(string sessionid, string taskid)
    {
        // TODO: remove after testing
        if (sessionid == "63fe288368974192c27a5388")
            return true;

        var state = (await Apis.DefaultWithSessionId(sessionid).GetTaskStateAsync(new TaskApi(taskid)))
            .ThrowIfError("").ThrowIfNull("");

        return state?.Type == TaskAction.GenerateTitleKeywords;
    }


    public record GenerateTitleKeywordsModel(string SessionId, string TaskId, string Title, string Keywords);
    public record CompleteModel(string SessionId, string TaskId, string Prompt);
}
