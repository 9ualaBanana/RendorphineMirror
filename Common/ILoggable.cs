namespace Common;

public interface ILoggable
{
    string LogName { get; }
}
public class Loggable : ILoggable
{
    public string LogName => GetNameFunc();
    readonly Func<string> GetNameFunc;

    public Loggable(Func<string> getNameFunc) => GetNameFunc = getNameFunc;
}