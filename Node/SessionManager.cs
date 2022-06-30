namespace Node;

public static class SessionManager
{
    const string AccountsEndpoint = Api.AccountsEndpoint;
    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;
    static string SessionId { get => Settings.SessionId!; set => Settings.SessionId = value!; }

    static ValueTask<OperationResult<string>> LoginAsync(string email, string password) =>
        Api.ApiPost<string>($"{TaskManagerEndpoint}/login", "sessionid", "Couldn't login.", ("email", email), ("password", password));
    static ValueTask<OperationResult<string>> RequestNicknameAsync() =>
        Api.ApiPost<string>($"{TaskManagerEndpoint}/generatenickname", "nickname", "Couldn't generate nickname.", ("sessionid", SessionId));

    public static ValueTask<OperationResult> RenameServerAsync(string name) =>
        Api.ApiPost($"{TaskManagerEndpoint}/renameserver", "Couldn't rename.", ("sessionid", SessionId), ("oldname", Settings.NodeName), ("newname", name));


    public static ValueTask<OperationResult> AuthAsync(string email, string password) =>
        LoginAsync(email, password)
        .Next(async sid =>
        {
            SessionId = sid;
            Settings.Email = email;

            if (Settings.NodeName is null)
            {
                var nickr = await RequestNicknameAsync().ConfigureAwait(false);
                if (nickr) Settings.NodeName = nickr.Value;
                else Settings.NodeName = Guid.NewGuid().ToString();
            }

            return true;
        });
}
