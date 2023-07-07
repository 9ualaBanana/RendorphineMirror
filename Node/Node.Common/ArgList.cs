namespace Node.Common;

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
    public void Add(IEnumerable<string?>? items) => Arguments.AddRange(WhereNotNull(items));

    public void Insert(int index, string? item)
    {
        if (item is not null)
            Arguments.Insert(index, item);
    }
    public void Insert(int index, params string?[]? items) => Insert(index, items?.AsEnumerable());
    public void Insert(int index, IEnumerable<string?>? items) => Arguments.InsertRange(index, WhereNotNull(items));

    static IEnumerable<string> WhereNotNull(IEnumerable<string?>? items) => (items?.Where(x => x is not null) ?? Enumerable.Empty<string>())!;


    IEnumerator<string> IEnumerable<string>.GetEnumerator() => Arguments.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Arguments.GetEnumerator();
}

public static class ArgListExtensions
{
    public static void AddArgumentIfNotNull<T>(this ArgList args, string prepend, T? value, Func<T, string>? tostring = null)
    {
        if (value is null) return;

        string? valuestr;
        if (tostring is not null)
            valuestr = tostring(value);
        else if (value is IFormattable formattable)
            valuestr = formattable.ToString(null, CultureInfo.InvariantCulture);
        else valuestr = value?.ToString();

        if (valuestr is null) return;
        args.Add(prepend);
        args.Add(valuestr);
    }
}
