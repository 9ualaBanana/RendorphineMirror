namespace Node.Tasks.Exec;

/// <summary> A class for an easy creation of an argument list </summary>
public class ArgList : IEnumerable<string>
{
    public int Count => Arguments.Count;

    readonly List<string> Arguments = new();

    public void Add(string item) => Arguments.Add(item);
    public void Add(params string[] items) => Arguments.AddRange(items);
    public void Add(IEnumerable<string> items) => Arguments.AddRange(items);

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => Arguments.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Arguments.GetEnumerator();
}
