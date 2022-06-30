namespace Common.Tasks;

public static class TaskList
{
    public static readonly ImmutableArray<PluginType> Types = Enum.GetValues<PluginType>().ToImmutableArray();
    public static ImmutableArray<IPluginAction> Actions;

    static TaskList()
    {
        var actions = new List<IPluginAction>();

        actions.Add(new PluginAction<MediaEditInfo>(PluginType.FFmpeg, "EditVideo"));
        actions.Add(new PluginAction<MediaEditInfo>(PluginType.FFmpeg, "EditRaster"));


        Actions = actions.ToImmutableArray();
    }

    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
}
