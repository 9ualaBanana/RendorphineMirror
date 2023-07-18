using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NLog.Web;
using Node.Common.Models;
using Node.Tasks.Models;
using NodeCommon;
using NodeCommon.Tasks;
using NodeCommon.Tasks.Model;


Initializer.AppName = "rapi";
Init.Initialize();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllers().AddNewtonsoftJson();
var s = builder.Services.AddControllers().Services;

var app = builder.Build();
app.MapPost("/api/login", Login);
app.MapPost("/api/registermytask", RegisterMyTask);
app.MapGet("/api/gettaskstate", GetTaskState);
app.MapGet("/api/getresult", GetResult);
app.MapGet("/api/finishtask", FinishTask);

app.Run();


async Task<string> Login(HttpRequest request)
{
    using var reader = new FormReader(request.Body);
    var form = await reader.ReadFormAsync();

    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var auth = await Api.Default.ApiPost<LoginResult>($"{Api.TaskManagerEndpoint}/login", null, "Logging in", ("email", email), ("password", password), ("guid", Guid.NewGuid().ToString()));
    if (!auth) return JsonApi.JsonFromOpResult(auth).ToString(Newtonsoft.Json.Formatting.None);

    return JsonApi.JsonFromOpResult(auth.Value.SessionId, "sessionid").ToString(Newtonsoft.Json.Formatting.None);
}
async Task<string> RegisterMyTask(HttpRequest request)
{
    using var reader = new FormReader(request.Body);
    var form = await reader.ReadFormAsync();

    var data = JObject.Parse(form["data"].ToString());
    var creationinfo = new TaskCreationInfo(
        data["type"].ThrowIfNull().Value<string>().ThrowIfNull(),
        JObject.Parse(form["input"].ToString()).ToObject<ITaskInputInfo>().ThrowIfNull(),
        JObject.Parse(form["output"].ToString()).ToObject<ITaskOutputInfo>().ThrowIfNull(),
        data,
        Enum.Parse<TaskPolicy>(form.GetValueOrDefault("policy", nameof(TaskPolicy.AllNodes)).ToString()),
        JObject.Parse(form["object"].ToString()).ToObject<TaskObject>().ThrowIfNull()
    )
    { PriceMultiplication = decimal.Parse(form.GetValueOrDefault("pricemul", "1").ToString()) };

    var register = await TaskRegistration.TaskRegisterAsync(creationinfo, form["sessionid"].ToString());
    if (!register) return JsonApi.JsonFromOpResult(register).ToString(Newtonsoft.Json.Formatting.None);

    return JsonApi.JsonFromOpResult(register.Value.Id, "taskid").ToString(Newtonsoft.Json.Formatting.None);
}

async Task<string> GetTaskState([FromQuery] string sessionid, [FromQuery] string taskid)
{
    var apis = Apis.DefaultWithSessionId(sessionid);

    var task = TaskApi.For(RegisteredTask.With(taskid));
    var shard = await apis.MaybeGetTaskShardAsync(taskid);
    if (!shard) return JsonApi.Error("Could not fetch task shard, try again later").ToString(Newtonsoft.Json.Formatting.None);

    var state = await apis.JustGetTaskStateAsync(task);
    if (!state) return JsonApi.JsonFromOpResult(state).ToString(Newtonsoft.Json.Formatting.None);

    if (state.Value is not null)
        return JsonApi.JsonFromOpResult(state.Value.State.AsOpResult(), "state").ToString(Newtonsoft.Json.Formatting.None);

    var finished = await apis.GetFinishedTasksStatesAsync(new[] { taskid });
    if (!finished) return JsonApi.JsonFromOpResult(finished).ToString(Newtonsoft.Json.Formatting.None);

    return JsonApi.JsonFromOpResult(finished.Value[taskid].State.AsOpResult(), "state").ToString(Newtonsoft.Json.Formatting.None);
}
async Task<string> GetResult([FromQuery] string sessionid, [FromQuery] string taskid, [FromQuery] string format)
{
    var extension = Enum.Parse<Extension>(format);
    if (extension is not (Extension.jpeg or Extension.eps or Extension.mov))
        return JsonApi.Error("Invalid format").ToString(Newtonsoft.Json.Formatting.None);

    var apis = Apis.DefaultWithSessionId(sessionid);
    var task = TaskApi.For(RegisteredTask.With(taskid));

    var getMplusItems = () => apis.ShardGet<ImmutableArray<ReceivedContentItemLite>>(task, "getmytaskmpitems", "items", "Getting task m+ results", ("sessionid", sessionid), ("taskid", taskid));
    var getUrls = async (ReceivedContentItemLite item) =>
        await apis.GetMPlusItemDownloadLinkAsync(task, item.Iid, extension)
            .Next(url => (item.Iid, new { filename = getitem(item.Files).Filename, size = getitem(item.Files).Size, url = url }).AsOpResult());

    var items = await getMplusItems()
        .Next(items => items.Select(getUrls).MergeDictResults());

    return JsonApi.JsonFromOpResult(items, "items").ToString(Newtonsoft.Json.Formatting.None);


    ReceivedContentItemLiteFile getitem(ReceivedContentItemLiteFiles files) => format switch
    {
        "jpeg" => files.Jpeg,
        "eps" => files.Eps.ThrowIfNull(),
        "mov" => files.Mov.ThrowIfNull(),
        _ => throw new Exception("Unknown format"),
    };
}
async Task<string> FinishTask([FromQuery] string sessionid, [FromQuery] string taskid)
{
    var apis = Apis.DefaultWithSessionId(sessionid);
    var task = TaskApi.For(RegisteredTask.With(taskid));

    var change = await apis.ChangeStateAsync(task, TaskState.Finished);
    return JsonApi.JsonFromOpResult(change).ToString(Newtonsoft.Json.Formatting.None);
}


record LoginResult(string SessionId, string UserId);

record ReceivedContentItemLite(string Iid, ReceivedContentItemLiteFiles Files);
record ReceivedContentItemLiteFiles(ReceivedContentItemLiteFile Jpeg, ReceivedContentItemLiteFile? Mov, ReceivedContentItemLiteFile? Eps)
{
    public ReceivedContentItemLiteFile File => Mov ?? Eps ?? Jpeg;
}
record ReceivedContentItemLiteFile(string Filename, long Size);