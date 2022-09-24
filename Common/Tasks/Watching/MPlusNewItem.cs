namespace Common.Tasks.Watching;

public record MPlusNewItem(string Iid, string UserId, MPlusNewItemFiles Files, long Registered);
public record MPlusNewItemFiles(MPlusNewItemFile Jpeg, MPlusNewItemFile? Mov = null);
public record MPlusNewItemFile(string FileName, long Size);
