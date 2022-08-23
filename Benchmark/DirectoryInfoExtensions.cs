namespace System.IO;

public static class DirectoryInfoExtensions
{
    public static T DeleteAfter<T>(this DirectoryInfo directory, Func<T> action, DeleteMode deleteMode = default)
    {
        var result = action();
        directory.Delete(deleteMode);
        return result;
    }

    public static T DeleteAfter<T>(this DirectoryInfo directory, Func<DirectoryInfo, T> action, DeleteMode deleteMode = default)
    {
        var result = action(directory);
        directory.Delete(deleteMode);
        return result;
    }

    public static async Task<T> DeleteAfterAsync<T>(this DirectoryInfo directory, Func<DirectoryInfo, Task<T>> action, DeleteMode deleteMode = default)
    {
        var result = await action(directory);
        directory.Delete(deleteMode);
        return result;
    }

    public static void Delete(this DirectoryInfo directory, DeleteMode deleteMode)
    {
        if (deleteMode is DeleteMode.NonRecursive) directory.Delete();
        else
        {
            if (deleteMode is DeleteMode.Wipe) directory.PrepareForWipe();
            directory.Delete(true);
        }
    }

    static void PrepareForWipe(this DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories).Where(file => file.IsReadOnly))
            file.IsReadOnly = false;
    }
}

public enum DeleteMode
{
    Wipe,
    NonRecursive,
    Recursive,
}
