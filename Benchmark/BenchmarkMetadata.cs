using NLog;
using System.Reflection;

namespace Benchmark;

public static class BenchmarkMetadata
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static Assembly _assembly = Assembly.GetExecutingAssembly();

    public readonly static Version Version = ReadAssemblyVersionOrThrow();
    static Version ReadAssemblyVersionOrThrow()
    {
        var version = _assembly.GetName().Version;
        if (version is not null) return version;

        const string errorMessage = "Benchmark version is not specified";
        _logger.Fatal(errorMessage);
        throw new MissingMemberException(errorMessage, nameof(AssemblyName.Version));
    }
}
