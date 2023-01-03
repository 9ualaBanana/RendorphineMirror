using Newtonsoft.Json;

namespace Common.Tasks;

public static class TaskRegistration
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();
    public static event Action<DbTaskFullState> TaskRegistered = delegate { };

    public static async ValueTask<OperationResult<string>> RegisterAsync(TaskCreationInfo info, string? sessionId = default)
    {
        if (info.PriceMultiplication < 1) return OperationResult.Err("Could not create task with price multiplication being less than 1");

        var data = info.Data;
        var input = TaskModels.DeserializeInput(info.Input);
        var output = TaskModels.DeserializeOutput(info.Output);
        var taskobj = info.TaskObject ?? (await input.GetFileInfo());

        await input.InitializeAsync();
        await output.InitializeAsync();

        var values = new List<(string, string)>()
        {
            ("sessionid", sessionId ?? Settings.SessionId!),
            ("object", JsonConvert.SerializeObject(taskobj, JsonSettings.LowercaseIgnoreNull)),
            ("input", JsonConvert.SerializeObject(input, JsonSettings.LowercaseIgnoreNullTaskInOut)),
            ("output", JsonConvert.SerializeObject(output, JsonSettings.LowercaseIgnoreNullTaskInOut)),
            ("data", data.ToString(Formatting.None)),
            ("policy", info.Policy.ToString()),
            ("origin", string.Empty),
            ("pricemul", (info.PriceMultiplication - (info.PriceMultiplication % .1)).ToString()),
        };
        if (info.Version is not null)
        {
            var soft = new[] { new TaskSoftwareRequirement(info.Type.ToString().ToLowerInvariant(), ImmutableArray.Create(info.Version), null), };
            values.Add(("software", JsonConvert.SerializeObject(soft, JsonSettings.LowercaseIgnoreNull)));
        }

        _logger.Info("Registering task: {Task}", string.Join("; ", values.Skip(1).Select(x => x.Item1 + ": " + x.Item2)));
        var idr = await Api.ApiPost<string>($"{Api.TaskManagerEndpoint}/registermytask", "taskid", "Registering task", values.ToArray());
        var id = idr.ThrowIfError();

        _logger.Info("Task registered with ID {Id}", id);
        var placed = new DbTaskFullState(id, Settings.Guid, info.Policy, taskobj, input, output, data)
        {
            UserId = Settings.UserId,
            Registered = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        };
        TaskRegistered(placed);

        return id;
    }
}
