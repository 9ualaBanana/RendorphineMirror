namespace Common;

public static class SessionManager
{
    const string Endpoint = Api.TaskManagerEndpoint;
    static string SessionId { get => Settings.SessionId!; set => Settings.SessionId = value!; }

    static ValueTask<OperationResult<string>> LoginAsync(string email, string password, string guid) =>
        Api.ApiPost<string>($"{Endpoint}/login", "sessionid", ("email", email), ("password", password), ("guid", guid));
    static ValueTask<OperationResult<string>> AutoLoginAsync(string email, string guid) =>
        Api.ApiPost<string>($"{Endpoint}/autologin", "sessionid", ("email", email), ("guid", guid));

    static ValueTask<OperationResult<string>> RequestNicknameAsync() =>
        Api.ApiPost<string>($"{Endpoint}/generatenickname", "nickname", ("sessionid", SessionId));
    public static ValueTask<OperationResult> RenameServerAsync(string name) =>
        Api.ApiPost($"{Endpoint}/renameserver", ("sessionid", SessionId), ("oldname", Settings.NodeName), ("newname", name));


    public static ValueTask<OperationResult> AuthAsync(string email, string password) => AuthAsync(email, password, Guid.NewGuid().ToString());
    public static ValueTask<OperationResult> AuthAsync(string email, string password, string guid) =>
        LoginAsync(email, password, guid)
        .Next(async sid =>
        {
            SessionId = sid;
            Settings.Guid = guid;
            Settings.Email = email;

            if (Settings.NodeName is null)
            {
                var nickr = await RequestNicknameAsync().ConfigureAwait(false);
                if (nickr) Settings.NodeName = nickr.Value;
                else Settings.NodeName = email + "_" + guid;
            }

            return true;
        });
}
