using System.Reflection;

namespace Benchmark;

public static class BenchmarkMetadata
{
    readonly static Assembly _assembly = Assembly.GetExecutingAssembly();

    public static Version Version => _assembly.GetName().Version!;
}
