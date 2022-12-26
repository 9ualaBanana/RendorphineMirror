namespace Transport.Upload;

internal static class UserSessionDataHelpers
{
    internal static string WithGuid(this string fileName) =>
        $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
}
