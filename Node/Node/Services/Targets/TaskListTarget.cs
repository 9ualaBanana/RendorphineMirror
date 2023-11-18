using Node.Tasks.Exec.Actions;

namespace Node.Services.Targets;

public class TaskListTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        IOList.RegisterAll(builder);
        registerTasks();


        void registerTasks()
        {
            void register<T>() where T : IPluginActionInfo =>
                builder.RegisterType<T>()
                    .Keyed<IPluginActionInfo>(Enum.Parse<TaskAction>(typeof(T).Name))
                    .SingleInstance();

            register<EditRaster>();
            register<EditVideo>();
            register<EsrganUpscale>();
            register<GreenscreenBackground>();
            register<VeeeVectorize>();
            register<GenerateQSPreview>();
            register<GenerateTitleKeywords>();
            //registertask<GenerateImageByMeta>();
            register<GenerateImageByPrompt>();
            register<Topaz>();
            register<GenerateAIVoice>();
        }
    }

    public IReadOnlyList<IPluginActionInfo> Actions => _Actions;
    readonly List<IPluginActionInfo> _Actions = new();

    public required IComponentContext Container { get; init; }

    public async Task ExecuteAsync() => _Actions.AddRange(Container.ResolveAllKeyed<IPluginActionInfo, TaskAction>());
}
