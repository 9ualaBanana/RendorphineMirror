using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Model;

public interface ITaskInputOutputInfo
{
    TaskInputOutputType Type { get; }

    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
public interface ITaskInputInfo : ITaskInputOutputInfo { }
public interface ITaskOutputInfo : ITaskInputOutputInfo { }


public static class TaskInputOutputInfo
{
    public static ImmutableDictionary<TaskInputOutputType, Type> Inputs;
    public static ImmutableDictionary<TaskInputOutputType, Type> Outputs;

    static TaskInputOutputInfo()
    {
        // FormatterServices.GetSafeUninitializedObject is being used to create valid object for getting only the .Type property
        // since all TaskInOutputInfo object implement ITaskInOutputInfo.Type property using `=>` and not `{ get; } =`

        var types = typeof(ITaskInputInfo).Assembly.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract)
            .Where(x => x.IsAssignableTo(typeof(ITaskInputInfo)) || x.IsAssignableTo(typeof(ITaskOutputInfo)))
            .Select(FormatterServices.GetSafeUninitializedObject)
            .Cast<ITaskInputOutputInfo>()
            .ToArray();
        
        Inputs = types.OfType<ITaskInputInfo>().ToImmutableDictionary(x => x.Type, x => x.GetType());
        Outputs = types.OfType<ITaskOutputInfo>().ToImmutableDictionary(x => x.Type, x => x.GetType());
    }


    public static ITaskInputInfo DeserializeInput(JObject input) => (ITaskInputInfo) input.ToObject(TaskInputOutputInfo.Inputs[input.GetValue("type", StringComparison.OrdinalIgnoreCase)!.ToObject<TaskInputOutputType>()])!;
    public static ITaskOutputInfo DeserializeOutput(JObject output) => (ITaskOutputInfo) output.ToObject(TaskInputOutputInfo.Outputs[output.GetValue("type", StringComparison.OrdinalIgnoreCase)!.ToObject<TaskInputOutputType>()])!;
}