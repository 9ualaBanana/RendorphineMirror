namespace Node.Tasks;

public class EitherTaskInput<T1, T2>
    where T1 : notnull
    where T2 : notnull
{
    public T1? Value1 { get; }
    public T2? Value2 { get; }

    public EitherTaskInput(T1 value1) => Value1 = value1;
    public EitherTaskInput(T2 value2) => Value2 = value2;

    public async Task<T> If<T>(Func<T1, Task<T>> first, Func<T2, Task<T>> second)
    {
        if (Value1 is not null) return await first(Value1);
        if (Value2 is not null) return await second(Value2);
        throw new Exception("Both values are null");
    }
}
public class EitherTaskInput<T1, T2, T3>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
{
    public T1? Value1 { get; }
    public T2? Value2 { get; }
    public T3? Value3 { get; }

    public EitherTaskInput(T1 value1) => Value1 = value1;
    public EitherTaskInput(T2 value2) => Value2 = value2;
    public EitherTaskInput(T3 value3) => Value3 = value3;

    public async Task<T> If<T>(Func<T1, Task<T>> first, Func<T2, Task<T>> second, Func<T3, Task<T>> third)
    {
        if (Value1 is not null) return await first(Value1);
        if (Value2 is not null) return await second(Value2);
        if (Value3 is not null) return await third(Value3);
        throw new Exception("Both values are null");
    }
}
