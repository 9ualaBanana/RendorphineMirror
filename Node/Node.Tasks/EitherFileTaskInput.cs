using System.Collections;

namespace Node.Tasks;

public class EitherFileTaskInput<T> : EitherTaskInput<TaskFileInput, T>, IReadOnlyTaskFileList
    where T : notnull
{
    public int Count => Value1?.Count ?? 0;

    public EitherFileTaskInput(TaskFileInput value1) : base(value1) { }
    public EitherFileTaskInput(T value2) : base(value2) { }

    public IEnumerator<FileWithFormat> GetEnumerator() => (Value1 ?? Enumerable.Empty<FileWithFormat>()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
