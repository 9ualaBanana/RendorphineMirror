namespace Node.Tasks;

public class EitherTaskInput<T1, T2>
    where T1 : notnull
    where T2 : notnull
{
    public T1? Value1 { get; }
    public T2? Value2 { get; }

    public EitherTaskInput(T1 value1) => Value1 = value1;
    public EitherTaskInput(T2 value2) => Value2 = value2;

    public void If(Action<T1> first, Action<T2> second)
    {
        if (Value1 is not null) first(Value1);
        else if (Value2 is not null) second(Value2);
        else throw new Exception("Both values are null");
    }
    public T If<T>(Func<T1, T> first, Func<T2, T> second)
    {
        if (Value1 is not null) return first(Value1);
        else if (Value2 is not null) return second(Value2);
        else throw new Exception("Both values are null");
    }

    public async Task If(Func<T1, Task> first, Func<T2, Task> second)
    {
        if (Value1 is not null) await first(Value1);
        else if (Value2 is not null) await second(Value2);
        else throw new Exception("Both values are null");
    }
    public async Task<T> If<T>(Func<T1, Task<T>> first, Func<T2, Task<T>> second)
    {
        if (Value1 is not null) return await first(Value1);
        else if (Value2 is not null) return await second(Value2);
        else throw new Exception("Both values are null");
    }
}
