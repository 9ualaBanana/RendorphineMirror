using NLog;
using System.Reflection;

namespace Benchmark;

public static class BenchmarkMetadata
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static Assembly _assembly = Assembly.GetExecutingAssembly();

    public static Version Version
    {
        get
        {
            var version = _assembly.GetName().Version!;
            _logger.Info("Benchmark version: {Version}", version);
            return version;
        }
    }
}
