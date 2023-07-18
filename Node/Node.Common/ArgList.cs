namespace Node.Common;

/// <summary> A class for an easy creation of an argument list </summary>
public class ArgList : MultiList<string> { }

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
