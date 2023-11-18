using System.Collections;

namespace Node.Plugins.Models;

public interface IPluginList : IEnumerable<Plugin>
{
    IReadOnlyCollection<Plugin> Plugins { get; }
}
