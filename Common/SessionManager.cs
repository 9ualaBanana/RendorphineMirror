namespace Common;

public static class SessionManager
{
    const string Endpoint = Api.TaskManagerEndpoint;
    static string SessionId => Settings.SessionId!;

    static ValueTask<OperationResult<string>> LoginAsync(string email, string password, string guid) =>
        Api.ApiPost<string>($"{Endpoint}/login", "sessionid", "Couldn't login.", ("email", email), ("password", password), ("guid", guid));
    static ValueTask<OperationResult<string>> AutoLoginAsync(string email, string guid) =>
        Api.ApiPost<string>($"{Endpoint}/autologin", "sessionid", "Couldn't login.", ("email", email), ("guid", guid));

    static ValueTask<OperationResult<string>> RequestNicknameAsync() =>
        Api.ApiPost<string>($"{Endpoint}/generatenickname", "nickname", "Couldn't generate nickname.",  ("sessionid", SessionId));
    public static ValueTask<OperationResult> RenameServerAsync(string name) =>
        Api.ApiPost($"{Endpoint}/renameserver", "Couldn't rename.", ("sessionid", SessionId), ("oldname", Settings.NodeName), ("newname", name));


    public static ValueTask<OperationResult> AutoAuthAsync(string email) => AutoAuthAsync(email, Guid.NewGuid().ToString());
    public static ValueTask<OperationResult> AutoAuthAsync(string email, string guid) =>
        AutoLoginAsync(email, guid)
        .Next(sid => LoginSuccess(sid, email, guid, true));

    public static ValueTask<OperationResult> AuthAsync(string email, string password) => AuthAsync(email, password, Guid.NewGuid().ToString());
    public static ValueTask<OperationResult> AuthAsync(string email, string password, string guid) =>
        LoginAsync(email, password, guid)
        .Next(sid => LoginSuccess(sid, email, guid, false));

    static async ValueTask<OperationResult> LoginSuccess(string sid, string email, string guid, bool slave)
    {
        Settings.AuthInfo = new AuthInfo(sid, email, guid, slave);
        if (Settings.NodeName is null)
        {
            var nickr = await RequestNicknameAsync().ConfigureAwait(false);
            nickr.LogIfError();

            if (nickr) Settings.NodeName = nickr.Value;
            else Settings.NodeName = email + "_" + guid;
        }

        return true;
    }
}
