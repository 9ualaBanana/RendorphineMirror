using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace NodeCommon.Tasks;

public static class TaskRegistration
{
    static readonly JsonSerializerSettings LowercaseIgnoreNullTaskInOut = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = TaskInputOutputSerializationContract.Instance,
        Formatting = Formatting.None,
    };


    public static async ValueTask<OperationResult<TypedRegisteredTask>> RegisterAsync(TaskCreationInfo info, string sessionId, ILogger? log = null) =>
        await TaskRegisterAsync(info, sessionId, log).Next(task => TypedRegisteredTask.With(task.Id, Enum.Parse<TaskAction>(info.Action)).AsOpResult());
    public static async ValueTask<OperationResult<DbTaskFullState>> TaskRegisterAsync(TaskCreationInfo info, string sessionId, ILogger? log = null)
    {
        if (info.PriceMultiplication < 1) return OperationResult.Err("Could not create task with price multiplication being less than 1");

        var data = info.Data;
        var input = info.Input is null ? null : TaskModels.DeserializeInput(info.Input);
        var inputs = info.Inputs?.Select(input => TaskModels.DeserializeInput((JObject) input)).ToArray();
        var output = TaskModels.DeserializeOutput(info.Output);
        var taskobj = info.TaskObject.ThrowIfNull("Task object was not provided");
        var pricemul = Math.Floor(info.PriceMultiplication * 10) / 10; // intervals of 0.1

        await (input?.InitializeAsync() ?? ValueTask.CompletedTask);
        if (inputs is not null)
            foreach (var inp in inputs)
                await inp.InitializeAsync();

        await output.InitializeAsync();


        var values = new List<(string, string)>()
        {
            ("sessionid", sessionId),
            ("object", JsonConvert.SerializeObject(taskobj, JsonSettings.LowercaseIgnoreNull)),
            ("output", JsonConvert.SerializeObject(output, LowercaseIgnoreNullTaskInOut)),
            ("data", data.ToString(Formatting.None)),
            ("policy", info.Policy.ToString()),
            ("origin", string.Empty),
            ("pricemul", pricemul.ToString()),
        };

        if (input is not null)
            values.Add(("input", JsonConvert.SerializeObject(input, LowercaseIgnoreNullTaskInOut)));
        if (inputs is not null)
            values.Add(("inputs", JsonConvert.SerializeObject(info.Inputs, LowercaseIgnoreNullTaskInOut)));

        if (info.Next?.IsDefaultOrEmpty == false)
            values.Add(("next", JsonConvert.SerializeObject(info.Next.Value)));

        if (info.SoftwareRequirements?.IsDefaultOrEmpty == false)
            values.Add(("software", JsonConvert.SerializeObject(info.SoftwareRequirements.Value, JsonSettings.LowercaseIgnoreNull)));

        if (output is MPlusTaskOutputInfo mPlusOutput && mPlusOutput.AutoremoveTimer is not null)
            values.Add(("autoremovetimer", mPlusOutput.AutoremoveTimer.ToString()!));

        var logtext = $"Registering task: {string.Join("; ", values.Skip(1).Select(x => x.Item1 + ": " + x.Item2))}";
        log?.LogInformation(logtext);

        var idr = await Api.Default.ApiPost<string>($"{Api.TaskManagerEndpoint}/registermytask", "taskid", "Registering task", values.ToArray());
        if (!idr) return idr.GetResult();

        log?.LogInformation("Task registered with ID {Id}", idr.Value);
        return new DbTaskFullState(idr.Value, new TaskInfo(taskobj, output, data, info.Policy)
        {
            Input = input,
            Inputs = inputs,
        });
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
