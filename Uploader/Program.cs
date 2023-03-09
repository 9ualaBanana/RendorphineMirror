global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.IO.Compression;
global using Common;
global using Newtonsoft.Json.Linq;
global using UpdaterCommon;
using Uploader;


const string updaterFilesPath = "debian@t.microstock.plus:/home/debian/updater3/files";
var debugConstantArgs = ImmutableArray.Create("/p:DefineConstants=DBG");
var defaultActions = new Dictionary<string, ImmutableArray<IAction>>()
{
    ["node"] = new IAction[]
    {
        new BuildUploadNodeAction("renderfin-win", ProjectType.Release, "win7-x64", updaterFilesPath),
        new BuildUploadNodeAction("renderfin-lin", ProjectType.Release, "linux-x64", updaterFilesPath),
        // new BuildUploadNodeAction("renderfin-osx", ProjectType.Release, "osx-x64", updaterFilesPath),
    }.ToImmutableArray(),
    ["nodedbg"] = new IAction[]
    {
        new BuildUploadNodeAction("renderfin-dbg-win", ProjectType.Release, "win7-x64", updaterFilesPath, debugConstantArgs),
        new BuildUploadNodeAction("renderfin-dbg-lin", ProjectType.Release, "linux-x64", updaterFilesPath, debugConstantArgs),
        // new BuildUploadNodeAction("renderfin-dbg-osx", ProjectType.Release, "osx-x64", updaterFilesPath, debugConstantArgs),
    }.ToImmutableArray(),
    ["nodetest"] = new IAction[]
    {
        new BuildUploadNodeAction("renderfin-test-win", ProjectType.Release, "win7-x64", updaterFilesPath),
        //new BuildUploadNodeAction("renderfin-test-lin", ProjectType.Release, "linux-x64", updaterFilesPath),
        // new BuildUploadNodeAction("renderfin-dbg-osx", ProjectType.Release, "osx-x64", updaterFilesPath),
    }.ToImmutableArray(),

    ["updateserver"] = new IAction[]
    {
        new BuildUploadAction(ProjectType.Release, "UpdateServer", "linux-x64", "debian@t.microstock.plus:/home/debian/updater3"),
    }.ToImmutableArray(),
    ["rapi"] = new IAction[]
    {
        new BuildUploadAction(ProjectType.Release, "RApi", "linux-x64", "debian@51.91.57.112:/home/debian/rapi"),
    }.ToImmutableArray(),
}.ToImmutableDictionary();


ImmutableArray<IAction> actions;
var jactions = null as JArray;
var predefinedType = null as string;

if (args.Length == 0) predefinedType = "node";
else
{
    try { jactions = JArray.Parse(args[0]); }
    catch { predefinedType = args[0]; }
}


if (predefinedType is not null)
    actions = defaultActions[predefinedType];
else if (jactions is not null)
{
    var actiontypes = typeof(Program).Assembly.GetTypes()
        .Where(x => !x.IsAbstract && !x.IsInterface && x.IsAssignableTo(typeof(IAction)))
        .ToImmutableArray();

    var actionlist = new List<IAction>();
    foreach (JObject jaction in jactions)
    {
        var type = (jaction.Property("type", StringComparison.OrdinalIgnoreCase)?.Value?.Value<string>()).ThrowIfNull();
        var ctype = actiontypes.First(t => t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

        var action = (IAction) jaction.ToObject(ctype).ThrowIfNull("Could not deserialize project " + jaction);
        actionlist.Add(action);
    }

    actions = actionlist.ToImmutableArray();
}
else throw new InvalidOperationException();


ConsoleColor.Blue.WriteLine($"Actions:\n {string.Join("\n ", actions.AsEnumerable())}");
foreach (var action in actions)
{
    ConsoleColor.Blue.WriteLine($"Starting {action}");
    action.Invoke();
}

ConsoleColor.Blue.WriteLine("Done.");