namespace Transport.Upload;

internal static class UserSessionDataHelpers
{
    internal static string AsUnixTimestamp(this DateTime dateTime) =>
        new DateTimeOffset(dateTime).ToUnixTimeMilliseconds().ToString();

    internal static string WithGuid(this string fileName) =>
        $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
}
