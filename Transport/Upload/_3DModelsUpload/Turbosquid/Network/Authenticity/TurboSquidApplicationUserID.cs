namespace Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

internal static class TurboSquidApplicationUserID
{
    internal static string _Parse(string html)
    {
        const string userApplicationUidKey = "id=\"user_application_uid\" value=\"";
        int userApplicationUidStartIndex = html.IndexOf(userApplicationUidKey) + userApplicationUidKey.Length;
        int userApplicationUidEndIndex = html.IndexOf('"', userApplicationUidStartIndex);
        return html[userApplicationUidStartIndex..userApplicationUidEndIndex];
    }
}
