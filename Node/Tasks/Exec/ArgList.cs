namespace Node.Tasks.Exec;

/// <summary> A class for an easy creation of an argument list </summary>
public class ArgList : IEnumerable<string>
{
    public int Count => Arguments.Count;

    readonly List<string> Arguments = new();

    public void Add(string? item)
    {
        if (item is not null)
            Arguments.Add(item);
    }
    public void Add(params string?[]? items) => Add(items?.AsEnumerable());
    public void Add(IEnumerable<string?>? items) => Arguments.AddRange((items?.Where(x => x is not null) ?? Enumerable.Empty<string>())!);

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => Arguments.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Arguments.GetEnumerator();
}
