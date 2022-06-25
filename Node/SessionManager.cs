namespace Node;

public static class SessionManager
{
    const string _AccountsEndpoint = Api.AccountsEndpoint;
    const string _TaskManagerEndpoint = Api.TaskManagerEndpoint;
    static string _sessionId { get => Settings.SessionId!; set => Settings.SessionId = value!; }

    static ValueTask<OperationResult<string>> LoginAsync(string email, string password) =>
        Api.ApiPost<string>($"{_TaskManagerEndpoint}/login", "sessionid", ("email", email), ("password", password));
    static ValueTask<OperationResult<string>> RequestNicknameAsync() =>
        Api.ApiPost<string>($"{_TaskManagerEndpoint}/generatenickname", "nickname", ("sessionid", _sessionId));

    public static ValueTask<OperationResult> RenameServerAsync(string name) =>
        Api.ApiPost($"{_TaskManagerEndpoint}/renameserver", ("sessionid", _sessionId), ("oldname", Settings.NodeName), ("newname", name));


    public static ValueTask<OperationResult> AuthAsync(string email, string password) =>
        LoginAsync(email, password)
        .Next(async sid =>
        {
            _sessionId = sid;
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
