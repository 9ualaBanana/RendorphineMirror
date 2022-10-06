using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public static class TaskModels
{
    public static ImmutableDictionary<TaskInputType, Type> Inputs;
    public static ImmutableDictionary<TaskOutputType, Type> Outputs;
    public static ImmutableDictionary<WatchingTaskInputType, Type> WatchingInputs;
    public static ImmutableDictionary<WatchingTaskOutputType, Type> WatchingOutputs;

    static TaskModels()
    {
        var types = CreateUninitializedObjects<ITaskInputOutputInfo, ITaskInputInfo, ITaskOutputInfo>();
        var wtypes = CreateUninitializedObjects<IWatchingTaskInputOutputInfo, IWatchingTaskInputInfo, IWatchingTaskOutputInfo>();

        Inputs = types.OfType<ITaskInputInfo>().ToImmutableDictionary(x => x.Type, x => x.GetType());
        Outputs = types.OfType<ITaskOutputInfo>().ToImmutableDictionary(x => x.Type, x => x.GetType());

        WatchingInputs = wtypes.OfType<IWatchingTaskInputInfo>().ToImmutableDictionary(x => x.Type, x => x.GetType());
        WatchingOutputs = wtypes.OfType<IWatchingTaskOutputInfo>().ToImmutableDictionary(x => x.Type, x => x.GetType());


        // FormatterServices.GetSafeUninitializedObject is being used to create valid object for getting only the .Type property
        // since all TaskInOutputInfo object implement ITaskInOutputInfo.Type property using `=>` and not `{ get; } =`
        static T[] CreateUninitializedObjects<T, TInput, TOutput>() where TInput : T where TOutput : T =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(x => x.IsAssignableTo(typeof(T)))
                .Where(x => x.IsClass && !x.IsAbstract)
                .Where(x => x.IsAssignableTo(typeof(TInput)) || x.IsAssignableTo(typeof(TOutput)))
                .Select(FormatterServices.GetSafeUninitializedObject)
                .Cast<T>()
                .ToArray();
    }


    public static ITaskInputInfo DeserializeInput(JObject input) => Deserialize<ITaskInputInfo, TaskInputType>(input, Inputs);
    public static ITaskOutputInfo DeserializeOutput(JObject output) => Deserialize<ITaskOutputInfo, TaskOutputType>(output, Outputs);
    public static IWatchingTaskInputInfo DeserializeWatchingInput(JObject input) => Deserialize<IWatchingTaskInputInfo, WatchingTaskInputType>(input, WatchingInputs);
    public static IWatchingTaskOutputInfo DeserializeWatchingOutput(JObject output) => Deserialize<IWatchingTaskOutputInfo, WatchingTaskOutputType>(output, WatchingOutputs);

    static T Deserialize<T, TType>(JObject jobject, ImmutableDictionary<TType, Type> dict) where TType : struct, Enum =>
        (T) jobject.ToObject(dict[jobject.GetValue("type", StringComparison.OrdinalIgnoreCase)!.ToObject<TType>()])!;
}
