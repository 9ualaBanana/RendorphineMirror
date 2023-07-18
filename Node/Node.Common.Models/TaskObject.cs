namespace Node.Common.Models;

public record TaskObject(string FileName, long Size)
{
    public static TaskObject From(FileInfo file) => new(file.Name, file.Length);
}