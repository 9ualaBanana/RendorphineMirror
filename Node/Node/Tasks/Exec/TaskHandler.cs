using System.Runtime.Serialization;

namespace Node.Tasks.Exec;

public static class TaskHandler
{
    public static IEnumerable<ITaskInputHandler> InputHandlerList => InputHandlers.Values;
    public static IEnumerable<ITaskOutputHandler> OutputHandlerList => OutputHandlers.Values;

    static readonly Dictionary<TaskInputType, ITaskInputHandler> InputHandlers = new();
    static readonly Dictionary<TaskOutputType, ITaskOutputHandler> OutputHandlers = new();
    static readonly Dictionary<WatchingTaskInputType, Func<WatchingTask, IWatchingTaskInputHandler>> WatchingHandlers = new();


    public static void AutoInitializeHandlers()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes())
            .Where(x => x.IsAssignableTo(typeof(ITaskHandler)))
            .Where(x => x.IsClass && !x.IsAbstract);

        foreach (var type in types)
            AddHandler(type);
    }
    public static void AddHandler(Type type)
    {
        if (type.IsAssignableTo(typeof(ITaskInputHandler)))
        {
            var handler = (ITaskInputHandler) Activator.CreateInstance(type)!;
            InputHandlers[handler.Type] = handler;
        }
        if (type.IsAssignableTo(typeof(ITaskOutputHandler)))
        {
            var handler = (ITaskOutputHandler) Activator.CreateInstance(type)!;
            OutputHandlers[handler.Type] = handler;
        }
        if (type.IsAssignableTo(typeof(IWatchingTaskInputHandler)))
        {
            // FormatterServices.GetSafeUninitializedObject is being used to create valid object for getting only the .Type property
            // since all IWatchingTaskInputHandler object implement Type property using `=>` and not `{ get; } =`

            WatchingHandlers[((IWatchingTaskInputHandler) FormatterServices.GetSafeUninitializedObject(type)).Type] =
                task => (IWatchingTaskInputHandler) Activator.CreateInstance(type, new object?[] { task })!;
        }
    }


    public static ITaskInputHandler GetInputHandler(this TaskBase task) => task.Input.Type.GetHandler();
    public static ITaskOutputHandler GetOutputHandler(this TaskBase task) => task.Output.Type.GetHandler();
    public static ITaskInputHandler GetHandler(this TaskInputType type) => InputHandlers[type];
    public static ITaskOutputHandler GetHandler(this TaskOutputType type) => OutputHandlers[type];
    public static IWatchingTaskInputHandler CreateWatchingHandler(this WatchingTask task) => WatchingHandlers[task.Source.Type](task);
}
