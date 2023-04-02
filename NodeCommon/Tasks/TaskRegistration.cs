using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NodeCommon.Tasks;

public static class TaskRegistration
{
    static readonly JsonSerializerSettings LowercaseIgnoreNullTaskInOut = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = TaskInputOutputSerializationContract.Instance,
        Formatting = Formatting.None,
    };


    readonly static Logger _logger = LogManager.GetCurrentClassLogger();
    public static event Action<DbTaskFullState> TaskRegistered = delegate { };


    public static async ValueTask<OperationResult<TypedRegisteredTask>> RegisterAsync(TaskCreationInfo info, string sessionId) =>
        await TaskRegisterAsync(info, sessionId).Next(task => TypedRegisteredTask.With(task.Id, Enum.Parse<TaskAction>(info.Action)).AsOpResult());
    public static async ValueTask<OperationResult<DbTaskFullState>> TaskRegisterAsync(TaskCreationInfo info, string sessionId, ILoggable? log = null)
    {
        if (info.PriceMultiplication < 1) return OperationResult.Err("Could not create task with price multiplication being less than 1");

        var data = info.Data;
        var input = TaskModels.DeserializeInput(info.Input);
        var output = TaskModels.DeserializeOutput(info.Output);
        var taskobj = info.TaskObject.ThrowIfNull("Task object was not provided");

        await input.InitializeAsync();
        await output.InitializeAsync();

        var values = new List<(string, string)>()
        {
            ("sessionid", sessionId),
            ("object", JsonConvert.SerializeObject(taskobj, JsonSettings.LowercaseIgnoreNull)),
            ("input", JsonConvert.SerializeObject(input, LowercaseIgnoreNullTaskInOut)),
            ("output", JsonConvert.SerializeObject(output, LowercaseIgnoreNullTaskInOut)),
            ("data", data.ToString(Formatting.None)),
            ("policy", info.Policy.ToString()),
            ("origin", string.Empty),
            ("pricemul", ((decimal)info.PriceMultiplication - ((decimal)info.PriceMultiplication % .1m)).ToString()),
        };
    
        if (info.Next?.IsDefaultOrEmpty == false)
            values.Add(("next", JsonConvert.SerializeObject(info.Next.Value)));

        if (info.SoftwareRequirements?.IsDefaultOrEmpty == false)
            values.Add(("software", JsonConvert.SerializeObject(info.SoftwareRequirements.Value, JsonSettings.LowercaseIgnoreNull)));

        if (output is MPlusTaskOutputInfo mPlusOutput && mPlusOutput.AutoremoveTimer is not null)
            values.Add(("autoremovetimer", mPlusOutput.AutoremoveTimer.ToString()!));

        var logtext = $"Registering task: {string.Join("; ", values.Skip(1).Select(x => x.Item1 + ": " + x.Item2))}";
        if (log is not null) log.LogInfo(logtext);
        else _logger.Info(logtext);

        var idr = await Api.Default.ApiPost<string>($"{Api.TaskManagerEndpoint}/registermytask", "taskid", "Registering task", values.ToArray());
        if (!idr) return idr.GetResult();

        _logger.Info("Task registered with ID {Id}", idr.Value);
        var task = new DbTaskFullState(idr.Value, new TaskInfo(taskobj, input, output, data, info.Policy));
        TaskRegistered(task);

        return task;
    }



    class TaskInputOutputSerializationContract : DefaultContractResolver
    {
        public static readonly TaskInputOutputSerializationContract Instance = new();

        private TaskInputOutputSerializationContract() => NamingStrategy = new LowercaseNamingStragedy();

        protected override List<MemberInfo> GetSerializableMembers(Type objectType) =>
            base.GetSerializableMembers(objectType).Where(x => x.GetCustomAttribute<NonSerializableForTasksAttribute>() is null).ToList();


        class LowercaseNamingStragedy : NamingStrategy
        {
            protected override string ResolvePropertyName(string name) => name.ToLowerInvariant();
        }
    }
}
