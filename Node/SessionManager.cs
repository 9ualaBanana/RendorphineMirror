namespace Node;

public class SessionManager
{
    const string _AccountsEndpoint = Api.AccountsEndpoint;
    const string _TaskManagerEndpoint = Api.TaskManagerEndpoint;
    static string _sessionId { get => Settings.SessionId!; set => Settings.SessionId = value!; }

    ValueTask<OperationResult<string>> LoginAsync(string email, string password) =>
        Api.ApiPost<string>($"{_AccountsEndpoint}/login", "sessionid", ("email", email), ("password", password));
    ValueTask<OperationResult> CheckSessionAsync()
    {
        if (_sessionId is null) return OperationResult.Err().AsVTask();
        return Api.ApiPost($"{_AccountsEndpoint}/checksession", ("sessionid", _sessionId));
    }
    ValueTask<OperationResult<string>> RequestNicknameAsync() =>
        Api.ApiPost<string>($"{_TaskManagerEndpoint}/generatenickname", "nickname", ("sessionid", _sessionId));

    public ValueTask<OperationResult> RenameServerAsync(string name) =>
        Api.ApiPost($"{_TaskManagerEndpoint}/renameserver", ("sessionid", _sessionId), ("oldname", Settings.NodeName), ("newname", name));
    public ValueTask<OperationResult> Logout() =>
        Api.ApiPost($"{_AccountsEndpoint}/logout", ("sessionid", _sessionId));


    public ValueTask<OperationResult> AuthAsync(string email, string password) => LoginAsync(email, password).Next(_ => CheckAsync());
    public async ValueTask<OperationResult> CheckAsync()
    {
        if (_sessionId is null) return OperationResult.Err();

        var check = await CheckSessionAsync().ConfigureAwait(false);
        if (check)
        {
            if (Settings.NodeName is null)
            {
                var nickr = await RequestNicknameAsync().ConfigureAwait(false);

                if (nickr) Settings.NodeName = nickr.Value;
                else Settings.NodeName = Guid.NewGuid().ToString();
            }
        }

        return check;
    }
}
