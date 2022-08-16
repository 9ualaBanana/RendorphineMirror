using Newtonsoft.Json;

namespace Common.Tasks;

public static class TaskRegistration
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();
    public static event Action<PlacedTask> TaskRegistered = delegate { };

    public static async ValueTask<OperationResult<string>> RegisterAsync(TaskCreationInfo info)
    {
        var data = info.Data;
        var taskobj = new TaskObject("3_UGVlayAyMDIxLTA4LTA0IDEzLTI5", 12345678);
        var input = info.Input;
        var output = info.Output;

        var values = new List<(string, string)>()
        {
            ("sessionid", Settings.SessionId!),
            ("object", JsonConvert.SerializeObject(taskobj, JsonSettings.LowercaseIgnoreNull)),
            ("input", input.ToString(Formatting.None)),
            ("output", output.ToString(Formatting.None)),
            ("data", data.ToString(Formatting.None)),
            ("origin", string.Empty),
        };
        if (info.Version is not null)
        {
            var soft = new[] { new TaskSoftwareRequirement(info.Type.ToString().ToLowerInvariant(), ImmutableArray.Create(info.Version), null), };
            values.Add(("software", JsonConvert.SerializeObject(soft, JsonSettings.LowercaseIgnoreNull)));
        }

        _logger.Info("Registering task: {Task}", JsonConvert.SerializeObject(info));
        var idr = await Api.ApiPost<string>($"{Api.TaskManagerEndpoint}/registermytask", "taskid", "Registering task", values.ToArray());
        var id = idr.ThrowIfError();

        _logger.Info("Task registered with ID {Id}", id);
        TaskRegistered(new PlacedTask(id, info));
        return id;
    }
}
